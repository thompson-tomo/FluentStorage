using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO.Abstractions;
using FluentStorage.Enums;
using FluentStorage.Utils.Validation;

namespace FluentStorage.Storage.Files {
	/// <summary>
	/// Access a local file system directory as a FluentStorage store.
	/// </summary>
	internal class DiskStore : StoreBase {
		private readonly System.IO.Abstractions.IFileSystem _fileSystem;
		private readonly string _directoryFullName;
		private const string AttributesFileExtension = ".attr";

		/// <summary>
		/// Creates an instance in a specific disk directory
		/// <param name="directoryFullName">Root directory</param>
		/// </summary>
		public DiskStore(string directoryFullName)
			: this(directoryFullName, new FileSystem()) { }

		/// <summary>
		/// Creates an instance in a specific disk directory
		/// <param name="directoryFullName">Root directory</param>
		/// <param name="fileSystem">FileSystem abstraction</param>
		/// </summary>
		public DiskStore(string directoryFullName, System.IO.Abstractions.IFileSystem fileSystem) {
			if (directoryFullName == null)
				throw new ArgumentNullException(nameof(directoryFullName));

			_fileSystem = fileSystem;
			_directoryFullName = _fileSystem.Path.GetFullPath(directoryFullName);
		}

		public override bool IsFileSystem() {
			return true;
		}

		private string NormalizeFilePath(string fullPath, bool createIfNotExists = true) {
			//id can contain path separators
			fullPath = fullPath.Trim(StoragePath.PathSeparator);
			string[] parts = fullPath.Split(StoragePath.PathSeparator).Select(EncodePathPart).ToArray();
			string name = parts[parts.Length - 1];
			string dir;
			if (parts.Length == 1) {
				dir = _directoryFullName;
			}
			else {
				string extraPath = string.Join(StoragePath.PathSeparatorString, parts, 0, parts.Length - 1);

				fullPath = _fileSystem.Path.Combine(_directoryFullName, extraPath);

				dir = fullPath;
				if (!_fileSystem.Directory.Exists(dir) && createIfNotExists)
					_fileSystem.Directory.CreateDirectory(dir);
			}

			return _fileSystem.Path.Combine(dir, name);
		}

		private string NormalizeFolderPath(string path, bool createIfNotExists) {
			if (path == null) return _directoryFullName;
			string[] parts = StoragePath.Split(path);

			string fullPath = _directoryFullName;

			foreach (string part in parts) {
				fullPath = _fileSystem.Path.Combine(fullPath, part);
			}

			if (!_fileSystem.Directory.Exists(fullPath)) {
				if (createIfNotExists) {
					_fileSystem.Directory.CreateDirectory(fullPath);
				}
				else {
					return null;
				}
			}

			return fullPath;
		}

		/// <summary>
		/// Returns the list of blob names in this storage, optionally filtered by prefix
		/// </summary>
		public Task<List<StoreObject>> ListObjects(StorageListOptions options, CancellationToken cancellationToken = default) {
			if (options == null) options = new StorageListOptions();

			ArgValidator.AssertPrefix(options.FilePrefix);

			if (!_fileSystem.Directory.Exists(_directoryFullName)) return Task.FromResult<List<StoreObject>>(new List<StoreObject>());

			string fullPath = NormalizeFolderPath(options?.FolderPath, false);
			if (fullPath == null) return Task.FromResult<List<StoreObject>>(new List<StoreObject>());

			string[] fileIds = _fileSystem.Directory.GetFiles(
			   fullPath,
			   string.IsNullOrEmpty(options.FilePrefix)
				  ? "*"
				  : options.FilePrefix + "*",
			   options.Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

			string[] directoryIds = _fileSystem.Directory.GetDirectories(
				  fullPath,
				  string.IsNullOrEmpty(options.FilePrefix)
					 ? "*"
					 : options.FilePrefix + "*",
				  options.Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

			var result = new List<StoreObject>();
			result.AddRange(directoryIds.Select(id => ToBlobItem(id, StorageObjectType.Folder, options.IncludeAttributes)));
			result.AddRange(
			   fileIds.Where(fid => !fid.EndsWith(AttributesFileExtension)).Select(id => ToBlobItem(id, StorageObjectType.File, options.IncludeAttributes)));
			result = result
			   .Where(i => options.BrowseFilter == null || options.BrowseFilter(i))
			   .Take(options.MaxResults == null ? int.MaxValue : options.MaxResults.Value)
			   .ToList();
			return Task.FromResult<List<StoreObject>>(result);
		}

		private static string FormatFlags(FileAttributes fa) {
			return string.Join("",
			   fa.ToString().Split(',').Select(v => v.Trim().Substring(0, 1).ToUpper()).OrderBy(l => l));
		}

		private StoreObject ToBlobItem(string fullPath, StorageObjectType kind, bool includeMeta) {

			string relPath = fullPath.Substring(_directoryFullName.Length);
			relPath = relPath.Replace(_fileSystem.Path.DirectorySeparatorChar, StoragePath.PathSeparator);
			relPath = relPath.Trim(StoragePath.PathSeparator);
			relPath = StoragePath.PathSeparatorString + relPath;

			if (kind == StorageObjectType.File) {
				var fi = new FileInfo(fullPath);

				var obj = new StoreObject(relPath, kind);
				obj.Size = fi.Length;
				// Converting the local time to a DateTimeOffset will save the offset of UTC.
				obj.DateModified = fi.LastWriteTime;
				obj.DateCreated = fi.CreationTime;
				obj.TryAddProperties(
				   "IsReadOnly", fi.IsReadOnly.ToString(),
				   // Universal sortable ("u") is always the same regardless of culture.
				   "LastAccessTimeUtc", fi.LastAccessTimeUtc.ToString("u"),
				   "Attributes", FormatFlags(fi.Attributes));

				if (includeMeta) {
					AddMetadata(obj);
				}

				return obj;
			}
			else {
				var di = _fileSystem.DirectoryInfo.New(fullPath);

				var obj = new StoreObject(relPath, StorageObjectType.Folder);
				obj.DateModified = di.LastWriteTime;
				obj.DateCreated = di.CreationTime;
				obj.TryAddProperties(
				   "LastAccessTimeUtc", di.LastAccessTimeUtc.ToString("u"),
				   "Attributes", FormatFlags(di.Attributes));

				if (includeMeta) {
					AddMetadata(obj);
				}

				return obj;
			}
		}

		private Stream CreateStream(string fullPath, bool overwrite = true) {
			ArgValidator.AssertFullPath(fullPath);
			if (!_fileSystem.Directory.Exists(_directoryFullName)) _fileSystem.Directory.CreateDirectory(_directoryFullName);
			string path = NormalizeFilePath(fullPath);

			_fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(path));
			Stream s = overwrite ? _fileSystem.File.Create(path) : _fileSystem.File.OpenWrite(path);
			s.Seek(0, SeekOrigin.End);
			return s;
		}

		private static string EncodePathPart(string path) {
			return path;
		}

		public override async Task SetObject(string fullPath, Stream dataStream, string contentType, bool append, CancellationToken cancellationToken = default) {
			if (dataStream is null)
				throw new ArgumentNullException(nameof(dataStream));
			ArgValidator.AssertFullPath(fullPath);

			fullPath = StoragePath.Normalize(fullPath);

			using Stream stream = CreateStream(fullPath, !append);
			await dataStream.CopyToAsync(stream);
		}
		public override async Task SetObject(string fullPath, Stream dataStream, bool append, CancellationToken cancellationToken = default) {
			await SetObject(fullPath, dataStream, null, append, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Opens file and returns a readable file stream
		/// </summary>
		public override async Task<Stream> OpenRead(string fullPath, CancellationToken cancellationToken = default) {
			ArgValidator.AssertFullPath(fullPath);

			fullPath = StoragePath.Normalize(fullPath);

			string path = NormalizeFilePath(fullPath);

			// exit if file does not exist
			if (!_fileSystem.File.Exists(path)) return null;

			return _fileSystem.File.OpenRead(path);
		}

		/// <summary>
		/// Opens file and returns a writeable file stream
		/// </summary>
		public override async Task<Stream> OpenWrite(string fullPath, bool overwrite, CancellationToken cancellationToken = default) {
			ArgValidator.AssertFullPath(fullPath);

			fullPath = StoragePath.Normalize(fullPath);

			string path = NormalizeFilePath(fullPath);

			// exit if file exists and overwriting is disabled
			if (!overwrite && _fileSystem.File.Exists(path)) return null;

			return _fileSystem.File.OpenWrite(path);
		}

		public override Task<Stream> OpenRange(string fullPath,long offset,long length,CancellationToken cancellationToken = default) {
			ArgValidator.AssertFullPath(fullPath);

			fullPath = StoragePath.Normalize(fullPath);

			string path = NormalizeFilePath(fullPath);

			// exit if file does not exist
			if (!_fileSystem.File.Exists(path)) return null;

			Stream stream = _fileSystem.File.Open(path,FileMode.Open,FileAccess.Read,FileShare.Read);

			stream.Seek(offset, SeekOrigin.Begin);

			return Task.FromResult(stream);
		}

		public override bool IsSeekable() {
			return true;
		}
		/// <summary>
		/// Deletes multiple objects by its full path.
		/// </summary>
		public override async Task DeleteObjects(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			if (fullPaths == null) return;

			foreach (string fullPath in fullPaths) {
				await DeleteObject(fullPath);
			}
		}

		/// <summary>
		/// Deletes an object by its full path.
		/// </summary>
		/// <param name="fullPath">The full path.</param>
		/// <param name="client">The sftp client to use.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns></returns>
		public override async Task DeleteObject(string fullPath, CancellationToken cancellationToken = default) {
			if (fullPath == null) return;

			string path = NormalizeFilePath(StoragePath.Normalize(fullPath));

			if (_fileSystem.File.Exists(path)) {
				_fileSystem.File.Delete(path);
			}
			else if (_fileSystem.Directory.Exists(path)) {
				_fileSystem.Directory.Delete(path, true);
			}
		}

		/// <summary>
		/// Checks if files exist on disk
		/// </summary>
		public override async Task<List<bool>> ObjectsExists(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			var result = new List<bool>();

			if (fullPaths != null) {
				ArgValidator.AssertFullPaths(fullPaths);

				foreach (string fullPath in fullPaths) {
					bool exists = _fileSystem.File.Exists(NormalizeFilePath(StoragePath.Normalize(fullPath), false));
					result.Add(exists);
				}
			}

			return result;
		}

		/// <summary>
		/// Checks if a file exists on disk
		/// </summary>
		public override async Task<bool> ObjectExists(string fullPath, CancellationToken cancellationToken = default) {
			var result = new List<bool>();

			if (fullPath != null) {
				ArgValidator.AssertFullPath(fullPath);
				return _fileSystem.File.Exists(NormalizeFilePath(StoragePath.Normalize(fullPath), false));
			}
			return false;
		}

		public override async Task<StoreObject> GetObjectInfo(string path, CancellationToken cancellationToken = default) {
			return (await GetObjectsInfo(new List<string> { path }, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
		}
		public override Task<List<StoreObject>> GetObjectsInfo(IEnumerable<string> ids, CancellationToken cancellationToken = default) {
			var result = new List<StoreObject>();

			foreach (string blobId in ids) {
				ArgValidator.AssertFullPath(blobId);

				string filePath = NormalizeFilePath(blobId, false);

				if (!_fileSystem.File.Exists(filePath)) {
					result.Add(null);
					continue;
				}

				result.Add(ToBlobItem(filePath, StorageObjectType.File, true));
			}

			return Task.FromResult<List<StoreObject>>(result);
		}

		public override async Task SetObjectInfo(StoreObject obj, CancellationToken cancellationToken = default) {
			await SetObjectsInfo(new List<StoreObject> { obj }, cancellationToken).ConfigureAwait(false);
		}

		public override Task SetObjectsInfo(IEnumerable<StoreObject> blobs, CancellationToken cancellationToken = default) {
			ArgValidator.AssertFullPaths(blobs);

			foreach (StoreObject blob in blobs.Where(b => b != null)) {
				string blobPath = NormalizeFilePath(blob.FullPath);

				if (!_fileSystem.File.Exists(blobPath))
					continue;

				if (blob?.Metadata == null)
					continue;

				string attrPath = NormalizeFilePath(blob.FullPath) + AttributesFileExtension;
				_fileSystem.File.WriteAllBytes(attrPath, blob.AttributesToByteArray());
			}

			return Task.CompletedTask;
		}

		private void AddMetadata(StoreObject blob) {
			string path = NormalizeFilePath(StoragePath.Normalize(blob.FullPath));

			if (!_fileSystem.File.Exists(path)) return;

			var fi = _fileSystem.FileInfo.New(path);

			try {
				string attrFilePath = path + AttributesFileExtension;
				if (_fileSystem.File.Exists(attrFilePath)) {
					byte[] content = _fileSystem.File.ReadAllBytes(attrFilePath);
					blob.AppendAttributesFromByteArray(content);
				}
			}
			catch (IOException) {
				//sometimes files are locked, inaccessible etc.
			}
		}


		/// <summary>
		/// Creates a new folder.
		/// </summary>
		public override async Task CreateDirectory(string folderPath, bool force, CancellationToken cancellationToken = default) {
			ArgValidator.AssertFullPath(folderPath);

			folderPath = StoragePath.Normalize(folderPath);

			string path = NormalizeFilePath(folderPath);

			if (_fileSystem.Directory.Exists(path)) {
				return;
			}

			_fileSystem.Directory.CreateDirectory(path);

			await Task.CompletedTask;
		}

		/// <summary>
		/// Deletes a folder.
		/// </summary>
		public override async Task DeleteDirectory(string folderPath, bool recursive, CancellationToken cancellationToken = default) {
			ArgValidator.AssertFullPath(folderPath);

			folderPath = StoragePath.Normalize(folderPath);

			string path = NormalizeFolderPath(folderPath, false);

			if (_fileSystem.Directory.Exists(path))
				_fileSystem.Directory.Delete(path, recursive);

			await Task.CompletedTask;
		}

		/// <summary>
		/// Returns true if the specified directory exists.
		/// </summary>
		public override async Task<bool> DirectoryExists(string folderPath, CancellationToken cancellationToken = default) {
			ArgValidator.AssertFullPath(folderPath);

			folderPath = StoragePath.Normalize(folderPath);

			string path = NormalizeFolderPath(folderPath, false);

			await Task.CompletedTask;
			return _fileSystem.Directory.Exists(path);
		}

		/// <summary>
		/// Moves a directory.
		/// </summary>
		public override async Task MoveDirectory(string sourceFolderPath, string destinationFolderPath, CancellationToken cancellationToken = default) {
			ArgValidator.AssertFullPath(sourceFolderPath);
			ArgValidator.AssertFullPath(destinationFolderPath);

			sourceFolderPath = StoragePath.Normalize(sourceFolderPath);
			destinationFolderPath = StoragePath.Normalize(destinationFolderPath);

			string sourcePath = NormalizeFolderPath(sourceFolderPath, false);
			string destinationPath = NormalizeFolderPath(destinationFolderPath, false);

			if (_fileSystem.Directory.Exists(sourcePath)) {
				_fileSystem.Directory.Move(sourcePath, destinationPath);
			}

			await Task.CompletedTask;
		}

	}
}