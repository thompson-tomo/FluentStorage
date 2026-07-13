using FluentStorage.Enums;
using FluentStorage.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FluentStorage.Storage {
	/// <summary>
	/// The base class used for all FluentStorage stores, including cloud buckets and disk-type stores.
	/// It implements many high level API methods for file manipulation, upload and download.
	/// It helps the provider-specific stores provide concrete implementations for the vast API of IBucket.
	/// </summary>
	public abstract class StoreBase : IStore {

		private const int BufferSize = 81920;

		public virtual void Dispose() {

		}

		public virtual bool HasFileSystem() {
			return false;
		}

		public virtual Task<Stream> OpenRead(string fullPath, CancellationToken cancellationToken = default) {
			throw new NotSupportedException();
		}

		public virtual async Task<List<StoreObject>> ListObjects(StorageListOptions options = null, CancellationToken cancellationToken = default) {
			var result = new List<StoreObject>();
			if (options == null) options = new StorageListOptions();

			await ListInternal(options.FolderPath, options, result, cancellationToken).ConfigureAwait(false);

			if (options.MaxResults != null && result.Count > options.MaxResults.Value) {
				result = result.Take(options.MaxResults.Value).ToList();
			}

			return result;
		}

		protected virtual async Task<List<StoreObject>> ListPath(
		   string path, StorageListOptions options, CancellationToken cancellationToken) {
			throw new NotSupportedException();
		}

		protected virtual async Task ListInternal(string path, StorageListOptions options, List<StoreObject> container, CancellationToken cancellationToken) {
			List<StoreObject> chunk = await ListPath(path, options, cancellationToken).ConfigureAwait(false);

			if (options.BrowseFilter != null) {
				container.AddRange(chunk.Where(b => options.BrowseFilter(b)));
			}
			else {
				container.AddRange(chunk);
			}

			if (options.MaxResults != null && container.Count >= options.MaxResults.Value)
				return;

			if ((this is IStore) && options.Recurse) {
				await Task.WhenAll(
				   chunk.Where(c => c.IsFolder).ToList()
				   .Select(c => ListInternal(c.FullPath, options, container, cancellationToken))).ConfigureAwait(false);
			}
		}


		public virtual async Task SetObject(string fullPath, Stream sourceStream, string contentType, bool append, CancellationToken cancellationToken) {
			await SetObject(fullPath, sourceStream, null, append, cancellationToken).ConfigureAwait(false);
		}



		/// <summary>
		/// Returns the list of available files, excluding folders.
		/// </summary>
		/// <returns>List of blob IDs</returns>
		public virtual async Task<List<StoreObject>> ListFiles(StorageListOptions options,
		   CancellationToken cancellationToken = default) {
			List<StoreObject> all = await ListObjects(options, cancellationToken).ConfigureAwait(false);

			return all.Where(i => i != null && i.IsFile).ToList();
		}

		/// <summary>
		/// Returns the list of available blobs
		/// </summary>
		/// <param name="folderPath"><see cref="StorageListOptions.FolderPath"/></param>
		/// <param name="browseFilter"><see cref="StorageListOptions.BrowseFilter"/></param>
		/// <param name="filePrefix"><see cref="StorageListOptions.FilePrefix"/></param>
		/// <param name="recurse"><see cref="StorageListOptions.Recurse"/></param>
		/// <param name="recursionMode"><see cref="StorageListOptions.RecursionMode"/></param>
		/// <param name="numberOfRecursionThreads"><see cref="StorageListOptions.NumberOfRecursionThreads"/></param>
		/// <param name="maxResults"><see cref="StorageListOptions.MaxResults"/></param>
		/// <param name="includeAttributes"><see cref="StorageListOptions.IncludeAttributes"/></param>
		/// <returns>List of blob IDs</returns>
		public virtual async Task<List<StoreObject>> ListDirectory(string folderPath = null,
		   Func<StoreObject, bool> browseFilter = null,
		   string filePrefix = null,
		   bool recurse = false,
		   StorageRecursion recursionMode = StorageRecursion.Remote,
		   int numberOfRecursionThreads = StorageListOptions.MAX_THREADS,
		   int? maxResults = null,
		   bool includeAttributes = false,
		   CancellationToken cancellationToken = default) {
			var options = new StorageListOptions();
			if (folderPath != null)
				options.FolderPath = folderPath;
			if (browseFilter != null)
				options.BrowseFilter = browseFilter;
			if (filePrefix != null)
				options.FilePrefix = filePrefix;
			options.Recurse = recurse;
			options.RecursionMode = recursionMode;
			options.NumberOfRecursionThreads = numberOfRecursionThreads;
			if (maxResults != null)
				options.MaxResults = maxResults;
			options.IncludeAttributes = includeAttributes;

			return await ListObjects(options, cancellationToken).ConfigureAwait(false);
		}



		/// <summary>
		/// Reads blob content and converts to text in UTF-8 encoding
		/// </summary>
		/// <param name="fullPath">Blob id</param>
		/// <param name="textEncoding">Optional text encoding. When not specified, <see cref="UTF8Encoding"/> is used.</param>
		/// <returns></returns>
		public virtual async Task<string> GetText(
		   string fullPath,
		   Encoding textEncoding = null,
		   CancellationToken cancellationToken = default) {
			Stream src = await OpenRead(fullPath, cancellationToken).ConfigureAwait(false);
			if (src == null) return null;

			var ms = new MemoryStream();
			using (src) {
				await src.CopyToAsync(ms).ConfigureAwait(false);
			}

			return (textEncoding ?? Encoding.UTF8).GetString(ms.ToArray());
		}

		/// <summary>
		/// Converts text to blob content and writes to storage
		/// </summary>
		/// <param name="fullPath">Blob to write</param>
		/// <param name="text">Text to write, treated in UTF-8 encoding</param>
		/// <param name="textEncoding">Optional text encoding. When not specified, <see cref="UTF8Encoding"/> is used.</param>
		/// <returns></returns>
		public virtual async Task SetText(
		   string fullPath, string text,
		   Encoding textEncoding = null,
		   CancellationToken cancellationToken = default) {
			using (Stream s = text.ToMemoryStream(textEncoding ?? Encoding.UTF8)) {
				await SetObject(fullPath, s, null, false, cancellationToken).ConfigureAwait(false);
			}
		}



		/// <summary>
		/// Checks if blobs exists in the storage
		/// </summary>
		public virtual async Task<List<bool>> ObjectsExists(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			return (await (Task.WhenAll(fullPaths.Select(fp => ObjectExists(fp, cancellationToken))).ConfigureAwait(false))).ToList();
		}

		/// <summary>
		/// Checks if blobs exists in the storage
		/// </summary>
		public virtual async Task<bool> ObjectExists(string fullPath, CancellationToken cancellationToken = default) {
			throw new NotSupportedException();
		}

		/// <summary>
		/// Deletes a single blob or a folder recursively.
		/// </summary>
		/// <returns></returns>
		public virtual async Task DeleteObject(string fullPath, CancellationToken cancellationToken = default) {
			throw new NotSupportedException();
		}

		public virtual Task DeleteObjects(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			return Task.WhenAll(fullPaths.Select(fp => DeleteObject(fp, cancellationToken)));
		}

		/// <summary>
		/// Deletes a collection of blobs or folders
		/// </summary>
		public virtual async Task DeleteObjects(
		   IEnumerable<StoreObject> blobs,
		   CancellationToken cancellationToken = default) {
			await DeleteObjects(blobs.Select(b => b.FullPath), cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Gets basic blob metadata
		/// </summary>
		/// <returns>Blob metadata or null if blob doesn't exist</returns>
		public virtual async Task<StoreObject> GetObjectInfo(string fullPath, CancellationToken cancellationToken = default) {
			throw new NotSupportedException();
		}

		public virtual async Task<List<StoreObject>> GetObjectsInfo(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			return (await (Task.WhenAll(fullPaths.Select(fp => GetObjectInfo(fp, cancellationToken))).ConfigureAwait(false))).ToList();
		}

		public virtual Task SetObjectsInfo(IEnumerable<StoreObject> blobs, CancellationToken cancellationToken = default) {
			throw new NotSupportedException();
		}

		public virtual async Task SetObjectInfo(StoreObject obj, CancellationToken cancellationToken = default) {
			throw new NotImplementedException();
		}

		public virtual async Task SetObject(string fullPath, Stream dataStream, bool append, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Writes byte array to the target.
		/// </summary>
		public virtual async Task SetBytes(string fullPath, byte[] data, bool append = false, CancellationToken cancellationToken = default) {
			if (data == null) {
				throw new ArgumentNullException(nameof(data));
			}

			using (var source = new MemoryStream(data)) {
				await SetObject(fullPath, source, null, append, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Reads blob content as byte array
		/// </summary>
		public virtual async Task<byte[]> GetBytes(string fullPath, CancellationToken cancellationToken = default) {
			Stream src = await OpenRead(fullPath, cancellationToken).ConfigureAwait(false);
			if (src == null) return null;

			var ms = new MemoryStream();
			using (src) {
				await src.CopyToAsync(ms).ConfigureAwait(false);
			}

			return ms.ToArray();
		}



		/// <summary>
		/// Downloads blob to a stream
		/// </summary>
		/// <param name="fullPath">Blob ID, required</param>
		/// <param name="targetStream">Target stream to copy to, required</param>
		/// <exception cref="System.ArgumentNullException">Thrown when any parameter is null</exception>
		/// <exception cref="System.ArgumentException">Thrown when ID is too long. Long IDs are the ones longer than 50 characters.</exception>
		/// <exception cref="StorageException">Thrown when blob does not exist, error code set to <see cref="StorageErrorCode.NotFound"/></exception>
		public virtual async Task GetObject(
		   string fullPath, Stream targetStream, CancellationToken cancellationToken = default) {
			if (targetStream == null)
				throw new ArgumentNullException(nameof(targetStream));

			Stream src = await OpenRead(fullPath, cancellationToken).ConfigureAwait(false);
			if (src == null) return;

			using (src) {
				await src.CopyToAsync(targetStream, BufferSize, cancellationToken).ConfigureAwait(false);
			}
		}



		/// <summary>
		/// Downloads a blob to the local filesystem.
		/// </summary>
		/// <param name="fullPath">Blob ID to download</param>
		/// <param name="filePath">Full path to the local file to be downloaded to. If the file exists it will be recreated wtih blob data.</param>
		public virtual async Task DownloadObject(
		   string fullPath, string filePath, CancellationToken cancellationToken = default) {
			Stream src = await OpenRead(fullPath, cancellationToken).ConfigureAwait(false);
			if (src == null) return;

			using (src) {
				using (Stream dest = File.Create(filePath)) {
					await src.CopyToAsync(dest, BufferSize, cancellationToken).ConfigureAwait(false);
					await dest.FlushAsync().ConfigureAwait(false);
				}
			}
		}

		/// <summary>
		/// Uploads local file to the blob storage
		/// </summary>
		/// <param name="fullPath">Blob ID to create or overwrite</param>
		/// <param name="filePath">Path to local file</param>
		public virtual async Task UploadObject(
		   string fullPath, string filePath, CancellationToken cancellationToken = default) {
			using (Stream src = File.OpenRead(filePath)) {
				await SetObject(fullPath, src, null, false, cancellationToken).ConfigureAwait(false);
			}
		}



		/// <summary>
		/// Writes an object to blob storage using <see cref="JsonSerializer"/>
		/// </summary>
		/// <typeparam name="T">Objec type</typeparam>
		/// <param name="fullPath">Full path to blob</param>
		/// <param name="instance">Object instance to write</param>
		/// <param name="options">Optional serialiser options</param>
		/// <param name="encoding">Text encoding used to write to the blob storage, defaults to <see cref="UTF8Encoding"/></param>
		/// <returns></returns>
		public virtual async Task SetJson<T>(
		   string fullPath, T instance,
		   JsonSerializerOptions options = null,
		   Encoding encoding = null,
		   CancellationToken cancellationToken = default) {
			string jsonText = JsonSerializer.Serialize(instance, options);
			await SetText(fullPath, jsonText, encoding, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Reads an object from blob storage using <see cref="JsonSerializer"/>
		/// </summary>
		/// <param name="fullPath">Full path to blob</param>
		/// <param name="ignoreInvalidJson">When true, json that cannot be deserialised is ignored and method simply returns default value</param>
		/// <param name="options">Optional serialiser options</param>
		/// <param name="encoding">Text encoding used to write to the blob storage, defaults to <see cref="UTF8Encoding"/></param>
		/// <returns></returns>
		public virtual async Task<T> GetJson<T>(string fullPath,
		   bool ignoreInvalidJson = false,
		   JsonSerializerOptions options = null,
		   Encoding encoding = null,
		   CancellationToken cancellationToken = default) {
			string jsonText = await GetText(fullPath, encoding, cancellationToken).ConfigureAwait(false);
			if (string.IsNullOrEmpty(jsonText))
				return default;

			try {
				return JsonSerializer.Deserialize<T>(jsonText, options);
			}
			catch (JsonException) {
				if (ignoreInvalidJson)
					return default;

				throw;
			}
		}



		/// <summary>
		/// Copies blob to another storage
		/// </summary>
		/// <param name="blobId">Blob ID to copy</param>
		/// <param name="targetStorage">Target storage</param>
		/// <param name="newId">Optional, when specified uses this id in the target  If null uses the original ID.</param>
		public virtual async Task CopyObjectToBucket(
		   string blobId, IStore targetStorage, string newId, CancellationToken cancellationToken = default) {
			using (Stream src = await OpenRead(blobId, cancellationToken).ConfigureAwait(false)) {
				if (src == null)
					return;

				await targetStorage.SetObject(newId ?? blobId, src, false, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Calculates an MD5 hash of a blob. Comparing to <see cref="StoreObject.MD5"/> field, it always returns
		/// a hash, even if the underlying storage doesn't support it natively.
		/// </summary>
		public virtual async Task<string> GetObjectMD5(StoreObject blob, CancellationToken cancellationToken = default) {
			if (blob == null)
				throw new ArgumentNullException(nameof(blob));

			if (blob.MD5 != null)
				return blob.MD5;

			blob = await GetObjectInfo(blob.FullPath, cancellationToken).ConfigureAwait(false);

			if (blob.MD5 != null)
				return blob.MD5;

			//hash definitely not supported, calculate it manually

			using (Stream s = await OpenRead(blob.FullPath, cancellationToken).ConfigureAwait(false)) {
				if (s == null)
					return null;

				string hash = s.MD5().ToHexString();

				return hash;
			}
		}

		/// <summary>
		/// Rename a blob (folder, file etc.).
		/// </summary>
		public virtual async Task RenameObject(string oldPath, string newPath, CancellationToken cancellationToken = default) {
			if (oldPath is null)
				throw new ArgumentNullException(nameof(oldPath));
			if (newPath is null)
				throw new ArgumentNullException(nameof(newPath));

			//try to use extended client here
			if (this is IStore) {
				await this.RenameObject(oldPath, newPath, cancellationToken).ConfigureAwait(false);
			}
			else {
				//this needs to be done recursively
				foreach (StoreObject item in await ListDirectory(oldPath, recurse: true).ConfigureAwait(false)) {
					if (item.IsFile) {
						string renamedPath = item.FullPath.Replace(oldPath, newPath);

						await CopyObjectToBucket(item, this, renamedPath, cancellationToken).ConfigureAwait(false);
						await DeleteObject(item, cancellationToken).ConfigureAwait(false);
					}
				}

				//rename self
				await CopyObjectToBucket(oldPath, this, newPath, cancellationToken).ConfigureAwait(false);
				await DeleteObject(oldPath, cancellationToken).ConfigureAwait(false);
			}


		}


		/// <summary>
		/// Creates a new folder in this file system. Does nothing in cloud storage buckets.
		/// </summary>
		/// <param name="folderPath">Path to the new folder.</param>
		public virtual async Task CreateDirectory(string folderPath, bool force, CancellationToken cancellationToken = default) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Deletes a folder in this file system. Does nothing in cloud storage buckets.
		/// </summary>
		/// <param name="folderPath">Path to the new folder.</param>
		public virtual async Task DeleteDirectory(string folderPath, bool recursive, CancellationToken cancellationToken = default) {
			throw new NotImplementedException();
		}


		/// <summary>
		/// Gets information about the connected FTP/SFTP server.
		/// </summary>
		public virtual async Task<Dictionary<string, object>> GetServer(CancellationToken cancellationToken = default) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns true if the specified directory or virtual directory exists.
		/// </summary>
		public virtual async Task<bool> DirectoryExists(string folderPath, CancellationToken cancellationToken = default) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Moves a directory or virtual directory.
		/// </summary>
		public virtual async Task MoveDirectory(string sourceFolderPath, string destinationFolderPath, CancellationToken cancellationToken = default) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Renames a directory or virtual directory.
		/// </summary>
		/*public virtual async Task RenameDirectory(string folderPath, string newFolderName, CancellationToken cancellationToken = default) {
			throw new NotImplementedException();
		}*/

		/// <summary>
		/// Gets the CHMOD permissions of a file.
		/// </summary>
		public virtual async Task<int> GetFilePermissions(string filePath, CancellationToken cancellationToken = default) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Sets the CHMOD permissions of a file.
		/// </summary>
		public virtual async Task SetFilePermissions(string filePath, int permissions, CancellationToken cancellationToken = default) {
			throw new NotImplementedException();
		}

	}
}
