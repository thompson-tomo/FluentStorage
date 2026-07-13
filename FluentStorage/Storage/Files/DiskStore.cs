using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO.Abstractions;
using FluentStorage.Enums;

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
			: this(directoryFullName, new FileSystem())
		{ }

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

		/// <summary>
		/// Returns the list of blob names in this storage, optionally filtered by prefix
		/// </summary>
		public Task<List<StoreObject>> ListObjects(StorageListOptions options, CancellationToken cancellationToken) {
			if (options == null) options = new StorageListOptions();

			GenericValidation.CheckBlobPrefix(options.FilePrefix);

			if (!_fileSystem.Directory.Exists(_directoryFullName)) return Task.FromResult<List<StoreObject>>(new List<StoreObject>());

			string fullPath = GetFolder(options?.FolderPath, false);
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

				var blob = new StoreObject(relPath, kind);
				blob.Size = fi.Length;
				// Converting the local time to a DateTimeOffset will save the offset of UTC.
				blob.DateModified = fi.LastWriteTime;
				blob.DateCreated = fi.CreationTime;
				blob.TryAddProperties(
				   "IsReadOnly", fi.IsReadOnly.ToString(),
				   // Universal sortable ("u") is always the same regardless of culture.
				   "LastAccessTimeUtc", fi.LastAccessTimeUtc.ToString("u"),
				   "Attributes", FormatFlags(fi.Attributes));

				if (includeMeta) {
					EnrichWithMetadata(blob);
				}

				return blob;
			}
			else {
				var di = _fileSystem.DirectoryInfo.New(fullPath);

				var blob = new StoreObject(relPath, StorageObjectType.Folder);
				blob.DateModified = di.LastWriteTime;
				blob.DateCreated = di.CreationTime;
				blob.TryAddProperties(
				   "LastAccessTimeUtc", di.LastAccessTimeUtc.ToString("u"),
				   "Attributes", FormatFlags(di.Attributes));

				if (includeMeta) {
					EnrichWithMetadata(blob);
				}

				return blob;
			}
		}

		private string GetFolder(string path, bool createIfNotExists) {
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

		private string GetFilePath(string fullPath, bool createIfNotExists = true) {
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

		private Stream CreateStream(string fullPath, bool overwrite = true) {
			GenericValidation.CheckBlobFullPath(fullPath);
			if (!_fileSystem.Directory.Exists(_directoryFullName)) _fileSystem.Directory.CreateDirectory(_directoryFullName);
			string path = GetFilePath(fullPath);

			_fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(path));
			Stream s = overwrite ? _fileSystem.File.Create(path) : _fileSystem.File.OpenWrite(path);
			s.Seek(0, SeekOrigin.End);
			return s;
		}

		private Stream OpenStream(string fullPath) {
			GenericValidation.CheckBlobFullPath(fullPath);
			string path = GetFilePath(fullPath);
			if (!_fileSystem.File.Exists(path)) return null;

			return _fileSystem.File.OpenRead(path);
		}

		private static string EncodePathPart(string path) {
			return path;
		}

		public override async Task SetObject(string fullPath, Stream dataStream, string contentType, bool append, CancellationToken cancellationToken) {
			if (dataStream is null)
				throw new ArgumentNullException(nameof(dataStream));
			GenericValidation.CheckBlobFullPath(fullPath);

			fullPath = StoragePath.Normalize(fullPath);

			using Stream stream = CreateStream(fullPath, !append);
			await dataStream.CopyToAsync(stream);
		}
		public override async Task SetObject(string fullPath, Stream dataStream, bool append, CancellationToken cancellationToken) {
			await SetObject(fullPath, dataStream, append, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Opens file and returns the open stream
		/// </summary>
		public override async Task<Stream> OpenRead(string fullPath, CancellationToken cancellationToken) {
			GenericValidation.CheckBlobFullPath(fullPath);

			fullPath = StoragePath.Normalize(fullPath);
			Stream result = OpenStream(fullPath);

			return result;
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

			string path = GetFilePath(StoragePath.Normalize(fullPath));

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
		public override async Task<List<bool>> ObjectsExists(IEnumerable<string> fullPaths, CancellationToken cancellationToken) {
			var result = new List<bool>();

			if (fullPaths != null) {
				GenericValidation.CheckBlobFullPaths(fullPaths);

				foreach (string fullPath in fullPaths) {
					bool exists = _fileSystem.File.Exists(GetFilePath(StoragePath.Normalize(fullPath), false));
					result.Add(exists);
				}
			}

			return result;
		}

		/// <summary>
		/// Checks if a file exists on disk
		/// </summary>
		public override async Task<bool> ObjectExists(string fullPath, CancellationToken cancellationToken) {
			var result = new List<bool>();

			if (fullPath != null) {
				GenericValidation.CheckBlobFullPath(fullPath);
				return _fileSystem.File.Exists(GetFilePath(StoragePath.Normalize(fullPath), false));
			}
			return false;
		}

		/// <summary>
		/// See interface
		/// </summary>
		public Task<List<StoreObject>> GetBlobsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) {
			var result = new List<StoreObject>();

			foreach (string blobId in ids) {
				GenericValidation.CheckBlobFullPath(blobId);

				string filePath = GetFilePath(blobId, false);

				if (!_fileSystem.File.Exists(filePath)) {
					result.Add(null);
					continue;
				}

				result.Add(ToBlobItem(filePath, StorageObjectType.File, true));
			}

			return Task.FromResult<List<StoreObject>>(result);
		}

		public Task SetBlobsAsync(IEnumerable<StoreObject> blobs, CancellationToken cancellationToken = default) {
			GenericValidation.CheckBlobFullPaths(blobs);

			foreach (StoreObject blob in blobs.Where(b => b != null)) {
				string blobPath = GetFilePath(blob.FullPath);

				if (!_fileSystem.File.Exists(blobPath))
					continue;

				if (blob?.Metadata == null)
					continue;

				string attrPath = GetFilePath(blob.FullPath) + AttributesFileExtension;
				_fileSystem.File.WriteAllBytes(attrPath, blob.AttributesToByteArray());
			}

			return Task.CompletedTask;
		}

		private void EnrichWithMetadata(StoreObject blob) {
			string path = GetFilePath(StoragePath.Normalize(blob.FullPath));

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

		public async Task RenameAsync(string oldPath, string newPath, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}
	}
}
