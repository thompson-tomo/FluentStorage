using FluentStorage.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FluentStorage.Storage {
	/// <summary>
	/// Interface to manage a single bucket across various cloud providers (AWS/Azure=/GCP/etc)
	/// The same interface is used to manage file system providers (Disk/FTP/FTPS).
	/// </summary>
	public interface IBucket : IDisposable {


		// ---------------------------------------------------------------------
		// Listing / Discovery
		// ---------------------------------------------------------------------

		/// <summary>
		/// Returns true if the given object storage is backed by a file system (Disk/FTP/FTPS).
		/// </summary>
		bool HasFileSystem();

		/// <summary>Returns the list of objects in this bucket.</summary>
		/// <param name="options">Listing options.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>The matching objects.</returns>
		Task<List<StorageObject>> ListObjects(StorageListOptions options = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Returns the list of objects in a specific directory of this bucket.
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
		Task<List<StorageObject>> ListDirectory(string folderPath = null, Func<StorageObject, bool> browseFilter = null,
		   string filePrefix = null, bool recurse = false,
		   StorageRecursion recursionMode = StorageRecursion.Remote,
		   int numberOfRecursionThreads = StorageListOptions.MAX_THREADS,
		   int? maxResults = null, bool includeAttributes = false,
		   CancellationToken cancellationToken = default);

		/// <summary>Returns the list of files, excluding folders.</summary>
		/// <param name="options">Listing options.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>The matching files.</returns>
		Task<List<StorageObject>> ListFiles(StorageListOptions options, CancellationToken cancellationToken = default);


		// ---------------------------------------------------------------------
		// Metadata & Existence
		// ---------------------------------------------------------------------

		/// <summary>Checks whether an object exists.</summary>
		/// <param name="fullPath">Full path of the object.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns><see langword="true"/> if the object exists; otherwise <see langword="false"/>.</returns>
		Task<bool> ObjectExists(string fullPath, CancellationToken cancellationToken = default);

		/// <summary>Checks if objects exist in the storage.</summary>
		/// <param name="fullPaths">Full paths of the objects.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>A collection indicating whether each object exists.</returns>
		Task<List<bool>> ObjectExists(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default);

		/// <summary>Gets metadata for a single object.</summary>
		/// <param name="fullPath">Full path of the object.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>The object metadata.</returns>
		Task<StorageObject> GetObjectInfo(string fullPath, CancellationToken cancellationToken = default);

		/// <summary>Gets object information which is useful for retrieving object metadata.</summary>
		/// <param name="fullPaths">Full paths of the objects.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>The object metadata.</returns>
		Task<List<StorageObject>> GetObjectsInfo(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default);

		/// <summary>Updates metadata for a single object.</summary>
		/// <param name="metadata">Object metadata.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task SetObjectInfo(StorageObject metadata, CancellationToken cancellationToken = default);

		/// <summary>Sets object information which is useful for setting object attributes (user metadata etc.).</summary>
		/// <param name="metadata">Object metadata.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task SetObjectsInfo(IEnumerable<StorageObject> metadata, CancellationToken cancellationToken = default);

		/// <summary>Returns the MD5 hash of an object.</summary>
		/// <param name="metadata">Object.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>The MD5 hash.</returns>
		Task<string> GetObjectMD5(StorageObject metadata, CancellationToken cancellationToken = default);


		// ---------------------------------------------------------------------
		// Read
		// ---------------------------------------------------------------------

		/// <summary>Opens the object stream for reading.</summary>
		/// <param name="fullPath">Full path of the object.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>An open readable stream.</returns>
		Task<Stream> OpenRead(string fullPath, CancellationToken cancellationToken = default);

		/// <summary>Copies an object into an existing stream.</summary>
		/// <param name="fullPath">Full path of the object.</param>
		/// <param name="targetStream">Destination stream.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task GetObject(string fullPath, Stream targetStream, CancellationToken cancellationToken = default);

		/// <summary>Reads an object into a byte array.</summary>
		/// <param name="fullPath">Full path of the object.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>The object contents.</returns>
		Task<byte[]> GetBytes(string fullPath, CancellationToken cancellationToken = default);

		/// <summary>Reads an object as text.</summary>
		/// <param name="fullPath">Full path of the object.</param>
		/// <param name="textEncoding">Text encoding. Defaults to UTF-8.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>The object contents.</returns>
		Task<string> GetText(string fullPath, Encoding textEncoding = null, CancellationToken cancellationToken = default);

		/// <summary>Reads and deserializes a JSON object.</summary>
		/// <param name="fullPath">Full path of the object.</param>
		/// <param name="ignoreInvalidJson">Whether invalid JSON should return the default value instead of throwing.</param>
		/// <param name="options">JSON serializer options.</param>
		/// <param name="encoding">Text encoding. Defaults to UTF-8.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>The deserialized object.</returns>
		Task<T> GetJson<T>(string fullPath, bool ignoreInvalidJson = false, JsonSerializerOptions options = null, Encoding encoding = null, CancellationToken cancellationToken = default);

		/// <summary>Downloads an object to a local file.</summary>
		/// <param name="fullPath">Full path of the object.</param>
		/// <param name="filePath">Destination file path.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task DownloadObject(string fullPath, string filePath, CancellationToken cancellationToken = default);


		// ---------------------------------------------------------------------
		// Write
		// ---------------------------------------------------------------------

		/// <summary>Uploads data to an object from a stream. Existing objects are overwritten.</summary>
		/// <param name="fullPath">Full path of the object.</param>
		/// <param name="dataStream">Source stream.</param>
		/// <param name="append">Whether to append to an existing object.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task SetObject(string fullPath, Stream dataStream, bool append = false, CancellationToken cancellationToken = default);

		/// <summary>Uploads data to an object from a stream. Existing objects are overwritten.</summary>
		/// <param name="fullPath">Full path of the object.</param>
		/// <param name="dataStream">Source stream.</param>
		/// <param name="contentType">MIME content type.</param>
		/// <param name="append">Whether to append to an existing object.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task SetObject(string fullPath, Stream dataStream, string contentType, bool append = false, CancellationToken cancellationToken = default);

		/// <summary>Writes a byte array to an object.</summary>
		/// <param name="fullPath">Full path of the object.</param>
		/// <param name="data">Data to write.</param>
		/// <param name="append">Whether to append to an existing object.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task SetBytes(string fullPath, byte[] data, bool append = false, CancellationToken cancellationToken = default);

		/// <summary>Writes text to an object.</summary>
		/// <param name="fullPath">Full path of the object.</param>
		/// <param name="text">Text to write.</param>
		/// <param name="textEncoding">Text encoding. Defaults to UTF-8.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task SetText(string fullPath, string text, Encoding textEncoding = null, CancellationToken cancellationToken = default);

		/// <summary>Writes an object as JSON.</summary>
		/// <param name="fullPath">Full path of the object.</param>
		/// <param name="instance">Object to serialize.</param>
		/// <param name="options">JSON serializer options.</param>
		/// <param name="encoding">Text encoding. Defaults to UTF-8.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task SetJson<T>(string fullPath, T instance, JsonSerializerOptions options = null, Encoding encoding = null, CancellationToken cancellationToken = default);

		/// <summary>Uploads a local file to an object.</summary>
		/// <param name="fullPath">Full path of the object.</param>
		/// <param name="filePath">Source file path.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task UploadObject(string fullPath, string filePath, CancellationToken cancellationToken = default);


		// ---------------------------------------------------------------------
		// Object Manipulation
		// ---------------------------------------------------------------------

		/// <summary>Copies an object to another bucket.</summary>
		/// <param name="blobId">Source object identifier.</param>
		/// <param name="targetStorage">Destination bucket.</param>
		/// <param name="newId">Destination object identifier.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task CopyObjectToBucket(string blobId, IBucket targetStorage, string newId, CancellationToken cancellationToken = default);

		/// <summary>Renames an object (file or folder).</summary>
		/// <param name="oldPath">Current path.</param>
		/// <param name="newPath">New path.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task RenameObject(string oldPath, string newPath, CancellationToken cancellationToken = default);

		/// <summary>Deletes a single object or folder.</summary>
		/// <param name="fullPath">Full path of the object or folder.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task DeleteObject(string fullPath, CancellationToken cancellationToken = default);

		/// <summary>Deletes an object by its full path.</summary>
		/// <param name="fullPaths">Full paths of the objects or folders.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task DeleteObject(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default);

		/// <summary>Deletes a collection of objects.</summary>
		/// <param name="blobs">Objects to delete.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task DeleteObjects(IEnumerable<StorageObject> blobs, CancellationToken cancellationToken = default);


		// ---------------------------------------------------------------------
		// File Systems Only
		// ---------------------------------------------------------------------

		/// <summary>Creates a new folder.</summary>
		/// <param name="folderPath">Path to the new folder.</param>
		Task CreateDirectory(string folderPath, string dummyFileName = null, string dummyFileContent = null, CancellationToken cancellationToken = default);

	}
}
