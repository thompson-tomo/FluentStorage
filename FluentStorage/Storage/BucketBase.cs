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
	public abstract class BucketBase : IBucket {

		private const int BufferSize = 81920;

		public virtual void Dispose() {

		}

		public virtual bool HasFileSystem() {
			return false;
		}

		public virtual Task<Stream> OpenReadAsync(string fullPath, CancellationToken cancellationToken = default) {
			throw new NotSupportedException();
		}

		public virtual async Task<List<StorageObject>> ListAsync(StorageListOptions options = null, CancellationToken cancellationToken = default) {
			var result = new List<StorageObject>();
			if (options == null) options = new StorageListOptions();

			await ListInternalAsync(options.FolderPath, options, result, cancellationToken).ConfigureAwait(false);

			if (options.MaxResults != null && result.Count > options.MaxResults.Value) {
				result = result.Take(options.MaxResults.Value).ToList();
			}

			return result;
		}

		protected virtual async Task<List<StorageObject>> ListPathAsync(
		   string path, StorageListOptions options, CancellationToken cancellationToken) {
			throw new NotSupportedException();
		}

		protected virtual async Task ListInternalAsync(string path, StorageListOptions options, List<StorageObject> container, CancellationToken cancellationToken) {
			List<StorageObject> chunk = await ListPathAsync(path, options, cancellationToken).ConfigureAwait(false);

			if (options.BrowseFilter != null) {
				container.AddRange(chunk.Where(b => options.BrowseFilter(b)));
			}
			else {
				container.AddRange(chunk);
			}

			if (options.MaxResults != null && container.Count >= options.MaxResults.Value)
				return;

			if ((this is IBucket) && options.Recurse) {
				await Task.WhenAll(
				   chunk.Where(c => c.IsFolder).ToList()
				   .Select(c => ListInternalAsync(c.FullPath, options, container, cancellationToken))).ConfigureAwait(false);
			}
		}

		public virtual Task DeleteAsync(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			return Task.WhenAll(fullPaths.Select(fp => DeleteSingleAsync(fp, cancellationToken)));
		}
		protected virtual Task DeleteSingleAsync(string fullPath, CancellationToken cancellationToken) {
			throw new NotSupportedException();
		}
		public virtual async Task<List<bool>> ExistsAsync(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			return (await (Task.WhenAll(fullPaths.Select(fp => ExistsAsync(fp, cancellationToken))).ConfigureAwait(false))).ToList();
		}

		public virtual async Task<List<StorageObject>> GetBlobsAsync(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			return (await (Task.WhenAll(fullPaths.Select(fp => GetBlobAsync(fp, cancellationToken))).ConfigureAwait(false))).ToList();
		}
		public virtual async Task WriteAsync(string fullPath, Stream sourceStream, string contentType, bool append, CancellationToken cancellationToken) {
			await WriteAsync(fullPath, sourceStream, null, append, cancellationToken).ConfigureAwait(false);
		}



		/// <summary>
		/// Returns the list of available files, excluding folders.
		/// </summary>
		/// <returns>List of blob IDs</returns>
		public virtual async Task<List<StorageObject>> ListFilesAsync(StorageListOptions options,
		   CancellationToken cancellationToken = default) {
			List<StorageObject> all = await ListAsync(options, cancellationToken).ConfigureAwait(false);

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
		public virtual async Task<List<StorageObject>> ListDirectoryAsync(string folderPath = null,
		   Func<StorageObject, bool> browseFilter = null,
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

			return await ListAsync(options, cancellationToken).ConfigureAwait(false);
		}



		/// <summary>
		/// Reads blob content and converts to text in UTF-8 encoding
		/// </summary>
		/// <param name="fullPath">Blob id</param>
		/// <param name="textEncoding">Optional text encoding. When not specified, <see cref="UTF8Encoding"/> is used.</param>
		/// <returns></returns>
		public virtual async Task<string> ReadTextAsync(
		   string fullPath,
		   Encoding textEncoding = null,
		   CancellationToken cancellationToken = default) {
			Stream src = await OpenReadAsync(fullPath, cancellationToken).ConfigureAwait(false);
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
		public virtual async Task WriteTextAsync(
		   string fullPath, string text,
		   Encoding textEncoding = null,
		   CancellationToken cancellationToken = default) {
			using (Stream s = text.ToMemoryStream(textEncoding ?? Encoding.UTF8)) {
				await WriteAsync(fullPath, s, null, false, cancellationToken).ConfigureAwait(false);
			}
		}



		/// <summary>
		/// Checks if blobs exists in the storage
		/// </summary>
		public virtual async Task<bool> ExistsAsync(string fullPath, CancellationToken cancellationToken = default) {
			IEnumerable<bool> r = await ExistsAsync(new[] { fullPath }, cancellationToken).ConfigureAwait(false);
			return r.First();
		}

		/// <summary>
		/// Deletes a single blob or a folder recursively.
		/// </summary>
		/// <returns></returns>
		public virtual async Task DeleteAsync(
		   string fullPath, CancellationToken cancellationToken = default) {
			await DeleteAsync(new[] { fullPath }, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Deletes a collection of blobs or folders
		/// </summary>
		public virtual async Task DeleteAsync(
		   IEnumerable<StorageObject> blobs,
		   CancellationToken cancellationToken = default) {
			await DeleteAsync(blobs.Select(b => b.FullPath), cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Gets basic blob metadata
		/// </summary>
		/// <returns>Blob metadata or null if blob doesn't exist</returns>
		public virtual async Task<StorageObject> GetBlobAsync(string fullPath, CancellationToken cancellationToken = default) {
			return (await GetBlobsAsync(new[] { fullPath }, cancellationToken).ConfigureAwait(false)).First();
		}

		/// <summary>
		/// Set blob attributes
		/// </summary>
		public virtual async Task SetBlobAsync(StorageObject blob, CancellationToken cancellationToken = default) {
			await SetBlobsAsync(new[] { blob }, cancellationToken).ConfigureAwait(false);
		}

		public virtual Task SetBlobsAsync(IEnumerable<StorageObject> blobs, CancellationToken cancellationToken = default) {
			throw new NotSupportedException();
		}


		public virtual async Task WriteAsync(string fullPath, Stream dataStream, bool append, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Writes byte array to the target.
		/// </summary>
		public virtual async Task WriteAsync(string fullPath, byte[] data, bool append = false, CancellationToken cancellationToken = default) {
			if (data == null) {
				throw new ArgumentNullException(nameof(data));
			}

			using (var source = new MemoryStream(data)) {
				await WriteAsync(fullPath, source, null, append, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Reads blob content as byte array
		/// </summary>
		public virtual async Task<byte[]> ReadBytesAsync(string fullPath, CancellationToken cancellationToken = default) {
			Stream src = await OpenReadAsync(fullPath, cancellationToken).ConfigureAwait(false);
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
		public virtual async Task ReadToStreamAsync(
		   string fullPath, Stream targetStream, CancellationToken cancellationToken = default) {
			if (targetStream == null)
				throw new ArgumentNullException(nameof(targetStream));

			Stream src = await OpenReadAsync(fullPath, cancellationToken).ConfigureAwait(false);
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
		public virtual async Task ReadToFileAsync(
		   string fullPath, string filePath, CancellationToken cancellationToken = default) {
			Stream src = await OpenReadAsync(fullPath, cancellationToken).ConfigureAwait(false);
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
		public virtual async Task WriteFileAsync(
		   string fullPath, string filePath, CancellationToken cancellationToken = default) {
			using (Stream src = File.OpenRead(filePath)) {
				await WriteAsync(fullPath, src, null, false, cancellationToken).ConfigureAwait(false);
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
		public virtual async Task WriteJsonAsync<T>(
		   string fullPath, T instance,
		   JsonSerializerOptions options = null,
		   Encoding encoding = null,
		   CancellationToken cancellationToken = default) {
			string jsonText = JsonSerializer.Serialize(instance, options);
			await WriteTextAsync(fullPath, jsonText, encoding, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Reads an object from blob storage using <see cref="JsonSerializer"/>
		/// </summary>
		/// <param name="fullPath">Full path to blob</param>
		/// <param name="ignoreInvalidJson">When true, json that cannot be deserialised is ignored and method simply returns default value</param>
		/// <param name="options">Optional serialiser options</param>
		/// <param name="encoding">Text encoding used to write to the blob storage, defaults to <see cref="UTF8Encoding"/></param>
		/// <returns></returns>
		public virtual async Task<T> ReadJsonAsync<T>(string fullPath,
		   bool ignoreInvalidJson = false,
		   JsonSerializerOptions options = null,
		   Encoding encoding = null,
		   CancellationToken cancellationToken = default) {
			string jsonText = await ReadTextAsync(fullPath, encoding, cancellationToken).ConfigureAwait(false);
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
		public virtual async Task CopyToAsync(
		   string blobId, IBucket targetStorage, string newId, CancellationToken cancellationToken = default) {
			using (Stream src = await OpenReadAsync(blobId, cancellationToken).ConfigureAwait(false)) {
				if (src == null)
					return;

				await targetStorage.WriteAsync(newId ?? blobId, src, false, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Calculates an MD5 hash of a blob. Comparing to <see cref="StorageObject.MD5"/> field, it always returns
		/// a hash, even if the underlying storage doesn't support it natively.
		/// </summary>
		public virtual async Task<string> GetMD5HashAsync(StorageObject blob, CancellationToken cancellationToken = default) {
			if (blob == null)
				throw new ArgumentNullException(nameof(blob));

			if (blob.MD5 != null)
				return blob.MD5;

			blob = await GetBlobAsync(blob.FullPath, cancellationToken).ConfigureAwait(false);

			if (blob.MD5 != null)
				return blob.MD5;

			//hash definitely not supported, calculate it manually

			using (Stream s = await OpenReadAsync(blob.FullPath, cancellationToken).ConfigureAwait(false)) {
				if (s == null)
					return null;

				string hash = s.MD5().ToHexString();

				return hash;
			}
		}

		/// <summary>
		/// Rename a blob (folder, file etc.).
		/// </summary>
		public virtual async Task RenameAsync(string oldPath, string newPath, CancellationToken cancellationToken = default) {
			if (oldPath is null)
				throw new ArgumentNullException(nameof(oldPath));
			if (newPath is null)
				throw new ArgumentNullException(nameof(newPath));

			//try to use extended client here
			if (this is IBucket) {
				await this.RenameAsync(oldPath, newPath, cancellationToken).ConfigureAwait(false);
			}
			else {
				//this needs to be done recursively
				foreach (StorageObject item in await ListDirectoryAsync(oldPath, recurse: true).ConfigureAwait(false)) {
					if (item.IsFile) {
						string renamedPath = item.FullPath.Replace(oldPath, newPath);

						await CopyToAsync(item, this, renamedPath, cancellationToken).ConfigureAwait(false);
						await DeleteAsync(item, cancellationToken).ConfigureAwait(false);
					}
				}

				//rename self
				await CopyToAsync(oldPath, this, newPath, cancellationToken).ConfigureAwait(false);
				await DeleteAsync(oldPath, cancellationToken).ConfigureAwait(false);
			}


		}



		/// <summary>
		/// Creates a new folder in this  If storage supports hierarchy, the folder is created as is, otherwise a folder is created by putting a dummy zero size file in that folder.
		/// </summary>
		/// <param name="folderPath">Path to the folder</param>
		/// <param name="dummyFileName">If storage doesn't support hierary, you can override the dummy file name created in that empty folder.</param>
		/// <returns></returns>
		public virtual async Task CreateFolderAsync(
		   string folderPath, string dummyFileName = null, string dummyFileContent = null, CancellationToken cancellationToken = default) {
			if (this is IBucket fileSystem) {
				await fileSystem.CreateFolderAsync(folderPath, null, null, cancellationToken).ConfigureAwait(false);
			}
			else {
				string fullPath = StoragePath.Combine(folderPath, dummyFileName ?? ".empty");

				// Check if the file already exists before we try to create it to prevent 
				// AccessDenied exceptions if two processes are creating the folder at the same time.
				if (await ExistsAsync(fullPath)) {
					return;
				}

				await WriteTextAsync(
				   fullPath,
				   dummyFileContent ?? "created as a workaround by FluentStorage when creating an empty parent folder",
				   null,
				   cancellationToken).ConfigureAwait(false);
			}
		}


	}
}
