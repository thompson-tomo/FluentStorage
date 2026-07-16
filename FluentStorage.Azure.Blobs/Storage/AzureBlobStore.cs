using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using FluentStorage.Azure.Blobs.Policy;
using FluentStorage.Azure.Blobs.Utils;
using FluentStorage.Enums;
using FluentStorage.Exceptions;
using FluentStorage.Model;
using FluentStorage.Storage;
using FluentStorage.Utils.Validation;
using MimeMapping;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HttpRange = Azure.HttpRange;

namespace FluentStorage.Azure.Blobs.Storage {
	/// <summary>
	/// Manages a single Azure Blob container.
	/// </summary>
	public class AzureBlobStore : StoreBase, IAzureBlobStore {

		private readonly BlobServiceClient _client;
		private readonly StorageSharedKeyCredential _sasSigningCredentials;
		private readonly string _containerName;
		private readonly ConcurrentDictionary<string, BlobContainerClient> _containerNameToContainerClient =
		   new ConcurrentDictionary<string, BlobContainerClient>();

		public AzureBlobStore(
		   BlobServiceClient blobServiceClient,
		   string accountName,
		   StorageSharedKeyCredential sasSigningCredentials = null,
		   string containerName = null) {
			_client = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
			_sasSigningCredentials = sasSigningCredentials;
			_containerName = containerName;

		}


		/// <summary>
		/// Returns the BlobServiceClient instance for this store.
		/// </summary>
		public override async Task<object> GetClient() {
			return _client;
		}

		public override async Task<List<StoreObject>> ListObjects(StorageListOptions options = null, CancellationToken cancellationToken = default) {
			if (options == null)
				options = new StorageListOptions();

			var result = new List<StoreObject>();
			var containers = new List<BlobContainerClient>();

			if (StoragePath.IsRootPath(options.FolderPath) && _containerName == null) {
				// list all of the containers
				containers.AddRange(await ListContainersAsync(cancellationToken).ConfigureAwait(false));
				result.AddRange(containers.Select(AzConvert.ToBlob));

				if (!options.Recurse)
					return result;
			}
			else {
				(BlobContainerClient container, string path) = await GetPartsAsync(options.FolderPath, false).ConfigureAwait(false);
				if (container == null)
					return new List<StoreObject>();
				options = options.Clone();
				options.FolderPath = path; //scan from subpath now
				containers.Add(container);
			}

			await Task.WhenAll(containers.Select(c => ListAsync(c, result, options, cancellationToken))).ConfigureAwait(false);

			if (options.MaxResults != null) {
				result = result.Take(options.MaxResults.Value).ToList();
			}

			return result;
		}


		public override async Task DeleteObjects(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			ArgValidator.AssertFullPaths(fullPaths);

			await Task.WhenAll(fullPaths.Select(fullPath => DeleteObjects(fullPath, cancellationToken))).ConfigureAwait(false);
		}

		public override async Task<List<bool>> ObjectsExists(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			return (await Task.WhenAll(fullPaths.Select(p => ObjectExists(p, cancellationToken))).ConfigureAwait(false)).ToList();
		}


		/// <summary>
		/// Opens an object for reading and returns its content stream.
		/// </summary>
		public override async Task<Stream> OpenRead(string fullPath, CancellationToken cancellationToken = default) {
			ArgValidator.AssertFullPath(fullPath);

			(BlobContainerClient container, string path) = await GetPartsAsync(fullPath, false).ConfigureAwait(false);

			BlockBlobClient client = container.GetBlockBlobClient(path);

			try {
				// Backward compatibility: Explicitly handle empty blobs to ensure they return a MemoryStream,
				// preserving the behavior of the old implementation.
				var properties = await client.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

				if (properties.Value.ContentLength == 0) {
					return new MemoryStream();
				}

				return await client.OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
			}
			catch (RequestFailedException ex) when (ex.ErrorCode == "BlobNotFound") {
				return null;
			}
		}


		/// <summary>
		/// Opens an object for writing and returns its content stream.
		/// Object will be written when the stream is disposed or flushed.
		/// </summary>
		public override async Task<Stream> OpenWrite(string fullPath, bool overwrite, CancellationToken cancellationToken = default) {
			ArgValidator.AssertFullPath(fullPath);

			// exit if file exists and overwriting is disabled
			if (!overwrite && await ObjectExists(fullPath, cancellationToken)) return null;

			(BlobContainerClient container, string path) = await GetPartsAsync(fullPath, true).ConfigureAwait(false);

			BlockBlobClient client = container.GetBlockBlobClient(path);

			try {
				return await client.OpenWriteAsync(overwrite, null, cancellationToken).ConfigureAwait(false);
			}
			catch (RequestFailedException ex) when (ex.ErrorCode == "OperationNotAllowedInCurrentState") {
				//happens when trying to write to a non-file object i.e. folder
			}

			return null;
		}

		/// <summary>
		/// Opens a readable stream beginning at the specified byte offset.
		/// </summary>
		public override async Task<Stream> OpenRange(string fullPath,long offset,long length, CancellationToken cancellationToken = default) {
			ArgValidator.AssertFullPath(fullPath);

			(BlobContainerClient container, string path) =
				await GetPartsAsync(fullPath, false).ConfigureAwait(false);

			BlockBlobClient client = container.GetBlockBlobClient(path);

			var response = await client.DownloadStreamingAsync(
				new HttpRange(offset, length), null, false, cancellationToken)
				.ConfigureAwait(false);

			return response.Value.Content;
		}

		public override bool IsSeekable() {
			return true;
		}
		/// <summary>
		/// Uploads a blob to Azure Blob storage, by automatically computing the Content-Type.
		/// </summary>
		public override async Task SetObject(string fullPath, Stream dataStream,
			bool append = false, CancellationToken cancellationToken = default) {
			await SetObject(fullPath, dataStream, null, append, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Uploads a blob to Azure Blob storage, with the given Content-Type.
		/// </summary>
		public override async Task SetObject(string fullPath, Stream dataStream,
			string contentType = null,
			bool append = false, CancellationToken cancellationToken = default) {
			ArgValidator.AssertFullPath(fullPath);

			if (dataStream == null)
				throw new ArgumentNullException(nameof(dataStream));

			(BlobContainerClient container, string path) = await GetPartsAsync(fullPath, true).ConfigureAwait(false);

			BlockBlobClient client = container.GetBlockBlobClient(path);

			// Auto compute a MIME type (content type) if not given
			if (contentType == null) {
				contentType = MimeUtility.GetMimeMapping(path);
			}

			try {
				var options = new BlobUploadOptions {
					HttpHeaders = new BlobHttpHeaders {
						ContentType = contentType
					}
				};
				await client.UploadAsync(
				   new StorageSourceStream(dataStream),
				   options: options,
				   cancellationToken: cancellationToken).ConfigureAwait(false);
			}
			catch (RequestFailedException ex) when (ex.ErrorCode == "OperationNotAllowedInCurrentState") {
				//happens when trying to write to a non-file object i.e. folder
			}
		}

		public override async Task<List<StoreObject>> GetObjectsInfo(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			return (await Task.WhenAll(fullPaths.Select(p => GetObjectInfo(p, cancellationToken))).ConfigureAwait(false)).ToList();
		}
		public override async Task SetObjectsInfo(IEnumerable<StoreObject> blobs, CancellationToken cancellationToken = default) {
			ArgValidator.AssertFullPaths(blobs);

			await Task.WhenAll(blobs.Select(b => SetObjectInfo(b, cancellationToken))).ConfigureAwait(false);
		}

		public override async Task SetObjectInfo(StoreObject blob, CancellationToken cancellationToken = default) {
			if (!await ObjectExists(blob, cancellationToken).ConfigureAwait(false))
				return;

			(BlobContainerClient container, string path) = await GetPartsAsync(blob, false).ConfigureAwait(false);

			if (string.IsNullOrEmpty(path)) {
				//it's a container!

				await container.SetMetadataAsync(blob.Metadata, cancellationToken: cancellationToken).ConfigureAwait(false);
			}
			else {
				BlockBlobClient client = container.GetBlockBlobClient(path);

				await client.SetMetadataAsync(blob.Metadata, cancellationToken: cancellationToken).ConfigureAwait(false);
			}
		}

		public override async Task<StoreObject> GetObjectInfo(string fullPath, CancellationToken cancellationToken = default) {
			(BlobContainerClient container, string path) = await GetPartsAsync(fullPath, false).ConfigureAwait(false);

			if (container == null)
				return null;

			if (string.IsNullOrEmpty(path)) {
				//it's a container

				Response<BlobContainerProperties> attributes = await container.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

				return AzConvert.ToBlob(container.Name, attributes);
			}

			BlobClient client = container.GetBlobClient(path);

			try {
				Response<BlobProperties> properties = await client.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

				return AzConvert.ToBlob(_containerName, path, properties);
			}
			catch (RequestFailedException ex) when (ex.ErrorCode == "BlobNotFound") {
				return null;
			}
		}



		public async Task<AzureStorageLease> AcquireLease(
		   string fullPath,
		   TimeSpan? maxLeaseTime = null,
		   string proposedLeaseId = null,
		   bool waitForRelease = false,
		   CancellationToken cancellationToken = default) {
			ArgValidator.AssertFullPath(fullPath);

			if (maxLeaseTime != null) {
				if (maxLeaseTime.Value < TimeSpan.FromSeconds(15) || maxLeaseTime.Value >= TimeSpan.FromMinutes(1)) {
					throw new ArgumentException(nameof(maxLeaseTime), $"When specifying lease time, make sure it's between 15 seconds and 1 minute, was: {maxLeaseTime.Value}");
				}
			}

			(BlobContainerClient container, string path) = await GetPartsAsync(fullPath, true).ConfigureAwait(false);

			//get lease client for container or blob
			BlobLeaseClient leaseClient;
			if (string.IsNullOrEmpty(path)) {
				leaseClient = container.GetBlobLeaseClient(proposedLeaseId);
			}
			else {
				//create a new blob if it doesn't exist
				if (!await ObjectExists(fullPath).ConfigureAwait(false)) {
					await SetObject(fullPath, new MemoryStream(), false, cancellationToken).ConfigureAwait(false);
				}

				BlockBlobClient client = container.GetBlockBlobClient(path);
				leaseClient = client.GetBlobLeaseClient(proposedLeaseId);
			}

			while (!cancellationToken.IsCancellationRequested) {
				try {
					await leaseClient.AcquireAsync(
					   maxLeaseTime == null ? TimeSpan.MinValue : maxLeaseTime.Value,
					   cancellationToken: cancellationToken).ConfigureAwait(false);

					break;
				}
				catch (RequestFailedException ex) when (ex.ErrorCode == "LeaseAlreadyPresent") {
					if (!waitForRelease) {
						throw new StorageException(StorageErrorCode.Conflict, ex);
					}
					else {
						await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
					}
				}
			}

			return new AzureStorageLease(leaseClient);
		}

		public async Task BreakLease(string fullPath, bool ignoreErrors = false, CancellationToken cancellationToken = default) {
			ArgValidator.AssertFullPath(fullPath);

			(BlobContainerClient container, string path) = await GetPartsAsync(fullPath, true).ConfigureAwait(false);

			//get lease client for container or blob
			BlobLeaseClient leaseClient;
			if (string.IsNullOrEmpty(path)) {
				leaseClient = container.GetBlobLeaseClient();
			}
			else {
				BlockBlobClient client = container.GetBlockBlobClient(path);
				leaseClient = client.GetBlobLeaseClient();
			}

			try {
				await leaseClient.BreakAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
			}
			catch (RequestFailedException ex) when (ex.ErrorCode == "LeaseNotPresentWithLeaseOperation") {
				if (!ignoreErrors)
					throw;
			}
			catch (RequestFailedException ex) when (ex.ErrorCode == "BlobNotFound") {
				if (!ignoreErrors)
					throw;
			}
		}

		public async Task<ContainerPublicAccessType> GetContainerPublicAccess(string containerName, CancellationToken cancellationToken = default) {
			(BlobContainerClient container, _) = await GetPartsAsync(containerName, true).ConfigureAwait(false);

			Response<BlobContainerAccessPolicy> policy =
			   await container.GetAccessPolicyAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

			return (ContainerPublicAccessType)(int)policy.Value.BlobPublicAccess;
		}

		public async Task SetContainerPublicAccess(string containerName, ContainerPublicAccessType containerPublicAccessType, CancellationToken cancellationToken = default) {
			(BlobContainerClient container, _) = await GetPartsAsync(containerName, true).ConfigureAwait(false);

			await container.SetAccessPolicyAsync(
			   (PublicAccessType)(int)containerPublicAccessType,
			   cancellationToken: cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Generate a Shared Access Signature for the storage account.
		/// </summary>
		public Task<string> GetStorageSas(
		   AccountSasPolicy accountPolicy, bool includeUrl = true, CancellationToken cancellationToken = default) {
			if (accountPolicy is null)
				throw new ArgumentNullException(nameof(accountPolicy));

			if (_sasSigningCredentials == null)
				throw new NotSupportedException($"cannot create Shared Access Signature, you have to authenticate using Shared Key in order to issue them.");

			string sas = accountPolicy.ToSasQuery(_sasSigningCredentials);

			if (includeUrl) {
				string url = _client.Uri.ToString();
				url += "?";
				url += sas;
				return Task.FromResult(url);
			}

			return Task.FromResult(sas);
		}

		/// <summary>
		/// Generate a Shared Access Signature for the blob container.
		/// </summary>
		public Task<string> GetContainerSas(
		   string containerName,
		   ContainerSasPolicy containerSasPolicy,
		   bool includeUrl = true,
		   CancellationToken cancellationToken = default) {
			string sas = containerSasPolicy.ToSasQuery(_sasSigningCredentials, containerName);

			if (includeUrl) {
				string url = _client.Uri.ToString();
				url += containerName;
				url += "/?";
				url += sas;
				return Task.FromResult(url);
			}

			return Task.FromResult(sas);
		}

		[Obsolete("Please use GetObjectSas or GetPresignedUrl instead.")]
		public async Task<string> GetBlobSas(
		   string fullPath,
		   BlobSasPolicy blobSasPolicy = null,
		   bool includeUrl = true,
		   CancellationToken cancellationToken = default) {
			if (blobSasPolicy == null)
				blobSasPolicy = new BlobSasPolicy(DateTime.UtcNow, TimeSpan.FromHours(1)) { Permissions = BlobSasPermission.Read };

			(BlobContainerClient container, string path) = await GetPartsAsync(fullPath, false).ConfigureAwait(false);

			string sas = blobSasPolicy.ToSasQuery(_sasSigningCredentials, container.Name, path);

			if (includeUrl) {
				string url = new Uri(_client.Uri, StoragePath.Normalize(fullPath)).ToString();
				url += "?";
				url += sas;
				return url;
			}

			return sas;
		}

		/// <summary>
		/// Generates a Shared Access Signature for a blob.
		/// </summary>
		/// <param name="fullPath">Full path of the object.</param>
		/// <param name="forDownload"><c>true</c> to generate a download URL; <c>false</c> to generate an upload URL.</param>
		/// <param name="https"><c>true</c> to require HTTPS; otherwise HTTP and HTTPS are permitted.</param>
		/// <param name="expiresInSeconds">Number of seconds until the URL expires.</param>
		/// <returns>The generated presigned URL.</returns>
		public override Task<string> GetPresignedUrl(string fullPath, bool forDownload, bool https, int expiresInSeconds = 86000) {

			return GetObjectSas(fullPath, new StorageUrlOptions {
				Permissions = forDownload
					? StorageUrlPermissions.Read
					: StorageUrlPermissions.Create | StorageUrlPermissions.Write,
				RequireHttps = https,
				ExpiresIn = TimeSpan.FromSeconds(expiresInSeconds)
			});
		}

		/// <summary>
		/// Generates a Shared Access Signature for a blob.
		/// </summary>
		/// <param name="fullPath">Full path of the object.</param>
		/// <param name="options">Options controlling permissions, expiration, protocol, and other Shared Access Signature settings.</param>
		/// <returns>The generated presigned URL.</returns>
		public override async Task<string> GetObjectSas(string fullPath, StorageUrlOptions options) {

			if (options == null)
				throw new ArgumentNullException(nameof(options));

			ArgValidator.AssertFullPath(fullPath);

			(BlobContainerClient container, string path) = await GetPartsAsync(fullPath, true).ConfigureAwait(false);

			BlockBlobClient client = container.GetBlockBlobClient(path);

			if (!client.CanGenerateSasUri)
				throw new NotSupportedException("Cannot create Shared Access Signature. The storage client must be authenticated using Shared Key.");

			BlobSasBuilder sas = AzConvert.OptionsToSas(options, container.Name, path);

			return client.GenerateSasUri(sas).ToString();
		}




		private async Task<List<BlobContainerClient>> ListContainersAsync(CancellationToken cancellationToken = default) {
			var r = new List<BlobContainerClient>();

			//check that the special "$logs" container exists
			BlobContainerClient logsContainerClient = _client.GetBlobContainerClient("$logs");
			Task<Response<BlobContainerProperties>> logsProps = logsContainerClient.GetPropertiesAsync();

			//in the meanwhile, enumerate
			await foreach (BlobContainerItem container in _client.GetBlobContainersAsync(BlobContainerTraits.Metadata).ConfigureAwait(false)) {
				(BlobContainerClient client, _) = await GetPartsAsync(container.Name, false).ConfigureAwait(false);

				if (client != null)
					r.Add(client);
			}

			try {
				await logsProps.ConfigureAwait(false);
				r.Add(logsContainerClient);
			}
			catch (RequestFailedException ex) when (ex.ErrorCode == "ContainerNotFound") {

			}

			return r;
		}

		private async Task ListAsync(BlobContainerClient container,
		   List<StoreObject> result,
		   StorageListOptions options,
		   CancellationToken cancellationToken = default) {
			using (var browser = new AzureContainerBrowser(container, _containerName == null, options.NumberOfRecursionThreads ?? StorageListOptions.MAX_THREADS)) {
				List<StoreObject> containerBlobs =
				   await browser.ListFolderAsync(options, cancellationToken)
					  .ConfigureAwait(false);

				if (containerBlobs.Count > 0) {
					result.AddRange(containerBlobs);
				}
			}
		}

		protected virtual async Task DeleteObjects(string fullPath, CancellationToken cancellationToken = default) {
			(BlobContainerClient container, string path) = await GetPartsAsync(fullPath, false).ConfigureAwait(false);

			if (StoragePath.IsRootPath(path)) {
				//deleting the entire container / filesystem
				await container.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
			}
			else {

				BlockBlobClient blob = string.IsNullOrEmpty(path)
				   ? null
				   : container.GetBlockBlobClient(StoragePath.Normalize(path));
				if (blob != null) {
					try {
						await blob.DeleteAsync(
						   DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken).ConfigureAwait(false);
					}
					catch (RequestFailedException ex) when (ex.ErrorCode == "BlobNotFound") {
						//this might be a folder reference, just try it

						await foreach (BlobItem recursedFile in
						   container.GetBlobsAsync(prefix: path, cancellationToken: cancellationToken).ConfigureAwait(false)) {
							BlobClient client = container.GetBlobClient(recursedFile.Name);
							await client.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
						}
					}
				}
			}
		}

		public override async Task<bool> ObjectExists(string fullPath, CancellationToken cancellationToken = default) {
			(BlobContainerClient container, string path) = await GetPartsAsync(fullPath, true).ConfigureAwait(false);

			if (container == null)
				return false;

			BlobBaseClient client = container.GetBlobBaseClient(path);

			try {
				await client.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
			}
			catch (RequestFailedException ex) when (ex.ErrorCode == "BlobNotFound") {
				return false;
			}

			return true;
		}

		private async Task<(BlobContainerClient, string)> GetPartsAsync(string fullPath, bool createContainer = true) {
			ArgValidator.AssertFullPath(fullPath);

			fullPath = StoragePath.Normalize(fullPath);
			if (fullPath == null)
				throw new ArgumentNullException(nameof(fullPath));

			string containerName, relativePath;

			if (_containerName == null) {
				string[] parts = StoragePath.Split(fullPath);

				if (parts.Length == 1) {
					containerName = parts[0];
					relativePath = string.Empty;
				}
				else {
					containerName = parts[0];
					relativePath = StoragePath.Combine(parts.Skip(1)).Substring(1);
				}
			}
			else {
				containerName = _containerName;
				relativePath = fullPath;
			}

			if (!_containerNameToContainerClient.TryGetValue(containerName, out BlobContainerClient container)) {
				container = _client.GetBlobContainerClient(containerName);
				if (_containerName == null) {
					try {
						//check if container exists
						await container.GetPropertiesAsync().ConfigureAwait(false);

					}
					catch (RequestFailedException ex) when (ex.ErrorCode == "ContainerNotFound") {
						if (createContainer) {
							await container.CreateIfNotExistsAsync().ConfigureAwait(false);
						}
						else {
							return (null, null);
						}
					}
				}

				_containerNameToContainerClient[containerName] = container;
			}

			return (container, relativePath);
		}

		/// <summary>
		/// Moves an object on the bucket. Returns true if it completed and false if it was skipped or the object did not exist.
		/// </summary>
		/// <param name="oldPath">Current object path.</param>
		/// <param name="newPath">New object path.</param>
		/// <param name="overwrite">Whether to overwrite the destination object if it already exists.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		public override async Task<bool> MoveObject(string oldPath, string newPath, bool overwrite, CancellationToken cancellationToken = default) {

			if (!await ObjectExists(oldPath, cancellationToken).ConfigureAwait(false))
				return false;

			if (!overwrite && await ObjectExists(newPath, cancellationToken).ConfigureAwait(false))
				return false;

			(BlobContainerClient sourceContainer, string sourcePath) = await GetPartsAsync(oldPath, false).ConfigureAwait(false);
			BlockBlobClient source = sourceContainer.GetBlockBlobClient(sourcePath);

			(BlobContainerClient destinationContainer, string destinationPath) = await GetPartsAsync(newPath, false).ConfigureAwait(false);
			BlockBlobClient destination = destinationContainer.GetBlockBlobClient(destinationPath);

			await destination.StartCopyFromUriAsync(source.Uri, cancellationToken: cancellationToken).ConfigureAwait(false);

			await source.DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

			return true;
		}

	}
}
