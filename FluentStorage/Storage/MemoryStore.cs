using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using FluentStorage.Streaming;
using FluentStorage.Utils.Extensions;
using FluentStorage.Enums;
using FluentStorage.Utils.Validation;
using FluentStorage.Model;

namespace FluentStorage.Storage {
	class MemoryStore : StoreBase {
		struct Tag {
			public StoreObject blob;
			public byte[] data;
		}

		private readonly Dictionary<string, Tag> _pathToTag = new Dictionary<string, Tag>();

		public override async Task<List<StoreObject>> ListObjects(StorageListOptions options, CancellationToken cancellationToken = default) {
			if (options == null) options = new StorageListOptions();

			IEnumerable<KeyValuePair<string, Tag>> query = _pathToTag;

			//limit by folder path
			if (options.Recurse) {
				if (!StoragePath.IsRootPath(options.FolderPath)) {
					string prefix = options.FolderPath + StoragePath.PathSeparatorString;

					query = query.Where(p => p.Key.StartsWith(prefix));
				}
			}
			else {
				var fPath = StoragePath.Normalize(options.FolderPath);
				query = query.Where(p => p.Value.blob.FolderPath == fPath);
			}

			//prefix
			query = query.Where(p => options.IsMatch(p.Value.blob));

			//browser filter
			query = query.Where(p => options.BrowseFilter == null || options.BrowseFilter(p.Value.blob));

			//limit
			if (options.MaxResults != null) {
				query = query.Take(options.MaxResults.Value);
			}

			List<StoreObject> matches = query.Select(p => p.Value.blob).ToList();

			return matches;
		}

		public override async Task SetObject(string fullPath, Stream sourceStream, bool append, CancellationToken cancellationToken = default) {
			await SetObject(fullPath, sourceStream, null, append, cancellationToken).ConfigureAwait(false);
		}

		public override async Task SetObject(string fullPath, Stream sourceStream, string contentType, bool append, CancellationToken cancellationToken = default) {
			if (fullPath == null) throw new ArgumentNullException(nameof(fullPath));
			fullPath = StoragePath.Normalize(fullPath);

			if (sourceStream is null)
				throw new ArgumentNullException(nameof(sourceStream));

			if (append) {
				if (!await ObjectExists(fullPath, cancellationToken)) {
					Write(fullPath, sourceStream);
				}
				else {
					Tag tag = _pathToTag[fullPath];
					byte[] data = tag.data.Concat(sourceStream.ToByteArray()).ToArray();
					Write(fullPath, new MemoryStream(data));
				}
			}
			else {
				Write(fullPath, sourceStream);
			}

		}

		public override async Task<Stream> OpenRead(string fullPath, CancellationToken cancellationToken = default) {
			if (fullPath == null) throw new ArgumentNullException(nameof(fullPath));
			fullPath = StoragePath.Normalize(fullPath);

			if (!_pathToTag.TryGetValue(fullPath, out Tag tag) || tag.data == null) return null;

			return new NonCloseableStream(new MemoryStream(tag.data));
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

			// delete "file"
			StoreObject pb = fullPath;
			if (_pathToTag.ContainsKey(pb)) {
				_pathToTag.Remove(pb);
			}

			string prefix = StoragePath.Normalize(fullPath) + StoragePath.PathSeparatorString;

			// delete all "files "under this "folder"
			List<StoreObject> candidates = _pathToTag.Where(p => p.Value.blob.FullPath.StartsWith(prefix)).Select(p => p.Value.blob).ToList();
			foreach (StoreObject candidate in candidates) {
				_pathToTag.Remove(candidate);
			}

		}

		public override async Task<List<bool>> ObjectsExists(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			var result = new List<bool>();

			foreach (string fullPath in fullPaths) {
				result.Add(_pathToTag.ContainsKey(StoragePath.Normalize(fullPath)));
			}

			return result;
		}

		public override async Task<StoreObject> GetObjectInfo(string path, CancellationToken cancellationToken = default) {
			return (await GetObjectsInfo(new List<string> { path }, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
		}
		public override async Task<List<StoreObject>> GetObjectsInfo(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			ArgValidator.AssertFullPaths(fullPaths);

			var result = new List<StoreObject>();

			foreach (string fullPath in fullPaths) {
				if (!_pathToTag.TryGetValue(StoragePath.Normalize(fullPath), out Tag tag)) {
					result.Add(null);
				}
				else {
					result.Add(tag.blob);
				}
			}

			return result;
		}

		public override async Task SetObjectInfo(StoreObject obj, CancellationToken cancellationToken = default) {
			await SetObjectsInfo(new List<StoreObject> { obj }, cancellationToken).ConfigureAwait(false);
		}

		public override async Task SetObjectsInfo(IEnumerable<StoreObject> blobs, CancellationToken cancellationToken = default) {
			if (blobs == null)
				return;

			foreach (StoreObject blob in blobs) {
				if (_pathToTag.TryGetValue(blob, out Tag tag)) {
					tag.blob.Metadata.Clear();
					tag.blob.Metadata.AddRange(blob.Metadata);
				}
			}
		}

		private void Write(string fullPath, Stream sourceStream) {
			if (fullPath == null) throw new ArgumentNullException(nameof(fullPath));
			fullPath = StoragePath.Normalize(fullPath);

			if (sourceStream is MemoryStream ms)
				ms.Position = 0;
			byte[] data = sourceStream.ToByteArray();

			if (!_pathToTag.TryGetValue(fullPath, out Tag tag)) {
				tag = new Tag {
					data = data,
					blob = new StoreObject(fullPath) {
						Size = data.Length,
						DateModified = DateTime.UtcNow,
						MD5 = data.MD5().ToHexString()
					}
				};
			}
			else {
				tag.data = data;
				tag.blob.Size = data.Length;
				tag.blob.DateModified = DateTime.UtcNow;
				tag.blob.MD5 = data.MD5().ToHexString();
			}
			_pathToTag[fullPath] = tag;

			AddVirtualFolderHierarchy(tag.blob);
		}

		private void AddVirtualFolderHierarchy(StoreObject fileBlob) {
			string path = fileBlob.FolderPath;

			while (!StoragePath.IsRootPath(path)) {
				var vf = new StoreObject(path, StorageObjectType.Folder);
				_pathToTag[path] = new Tag { blob = vf };

				path = StoragePath.GetParent(path);
			}
		}

		public override async Task<bool> ObjectExists(string fullPath, CancellationToken cancellationToken = default) {
			if (fullPath == null) throw new ArgumentNullException(nameof(fullPath));

			return _pathToTag.ContainsKey(fullPath);
		}

	}
}
