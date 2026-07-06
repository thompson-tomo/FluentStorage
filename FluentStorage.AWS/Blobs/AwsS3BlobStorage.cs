using FluentStorage.Blobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.S3;
using Amazon.Runtime;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System.Threading.Tasks;
using System.Threading;
using FluentStorage.Streaming;
using FluentStorage.Utils.Extensions;
using Amazon.S3.Util;
using MimeMapping;

#if !NET6_0_OR_GREATER

#endif
using System.Net;

namespace FluentStorage.AWS.Blobs {
	/// <summary>
	/// Amazon S3 storage adapter for blobs
	/// </summary>
	class AwsS3BlobStorage : IBlobStorage, IAwsS3BlobStorage {
		private const int ListChunkSize = 10;
		private readonly string _bucketName;
		private readonly AmazonS3Client _client;
		private readonly TransferUtility _fileTransferUtility;
		private bool _initialised = false;
		private bool _usePutObject = false;
		private bool _disablePayloadSigning = false;


		/// <summary>
		/// Returns reference to the native AWS S3 blob client.
		/// </summary>
		public IAmazonS3 NativeBlobClient => _client;

		/// <summary>
		/// Return bucket name.
		/// </summary>
		public string BucketName => _bucketName;

#if !NET16
		public static AwsS3BlobStorage FromAwsCliProfile(string profileName, string bucketName, string region) {
			return new AwsS3BlobStorage(bucketName, region, AwsCliCredentials.GetCredentials(profileName));
		}

		public static AwsS3BlobStorage FromAwsCredentials(AWSCredentials credentials, string bucketName, string region) {
			return new AwsS3BlobStorage(bucketName, region, credentials);
		}
		public static AwsS3BlobStorage FromDigitalOcean(string accessKeyId, string secretAccessKey, string bucketName, string digitalOceanRegion, string sessionToken = null) {
			var serviceUrl = $"https://{digitalOceanRegion}.digitaloceanspaces.com";
			return new AwsS3BlobStorage(accessKeyId, secretAccessKey, sessionToken, bucketName, null, serviceUrl);
		}
		public static AwsS3BlobStorage FromMinIO(string accessKeyId, string secretAccessKey, string bucketName, string awsRegion, string minioServerUrl, string sessionToken = null) {
			var config = new AmazonS3Config {
				AuthenticationRegion = awsRegion,
				ServiceURL = minioServerUrl,
				ForcePathStyle = true,
			};
			return new AwsS3BlobStorage(accessKeyId, secretAccessKey, sessionToken, bucketName, config);
		}
		public static AwsS3BlobStorage FromWasabi(string accessKeyId, string secretAccessKey, string bucketName, string wasabiServiceUrl, string sessionToken = null) {
			return new AwsS3BlobStorage(accessKeyId, secretAccessKey, sessionToken, bucketName, null, wasabiServiceUrl);
		}
		public static AwsS3BlobStorage FromCloudflareR2(string accessKeyId, string secretAccessKey, string bucketName, string cloudflareAccountId) {

			var config = new AmazonS3Config {
				// ServiceURL is always https://<account-id>.r2.cloudflarestorage.com.
				ServiceURL = $"https://{cloudflareAccountId}.r2.cloudflarestorage.com",
				// AuthenticationRegion = "auto" is the recommended value for R2
				AuthenticationRegion = "auto",
				// ForcePathStyle = false uses virtual-hosted style requests, which R2 supports and recommends
				ForcePathStyle = false,
				// UseHttp = false ensures HTTPS (the endpoint itself is HTTPS, but this makes it explicit).
				UseHttp = false,
			};

			var store = new AwsS3BlobStorage(accessKeyId, secretAccessKey, null, bucketName, config);
			store._usePutObject = true;
			store._disablePayloadSigning = true;
			return store;
		}
#endif

		// [ADD STORAGE PROVIDER]]


		public AwsS3BlobStorage(string bucketName, string region, AWSCredentials credentials) {
			_bucketName = bucketName;
			_client = new AmazonS3Client(credentials, CreateConfig(region, null));
			_fileTransferUtility = new TransferUtility(_client);
		}

		/// <summary>
		/// Creates a new instance of <see cref="AwsS3BlobStorage"/> for a given region endpoint, and will assume the running AWS ECS Task role credentials or Lambda role credentials.
		/// </summary>
		public AwsS3BlobStorage(string bucketName, string region) {
			_bucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
			_client = new AmazonS3Client(region.ToRegionEndpoint());
			_fileTransferUtility = new TransferUtility(_client);
		}

		/// <summary>
		/// Creates a new instance of <see cref="AwsS3BlobStorage"/> for a given region endpoint.
		/// </summary>
		public AwsS3BlobStorage(string accessKeyId, string secretAccessKey, string sessionToken, string bucketName, string region, string serviceUrl)
		   : this(accessKeyId, secretAccessKey, sessionToken, bucketName, CreateConfig(region, serviceUrl)) {
		}

		private static AmazonS3Config CreateConfig(string region, string serviceUrl) {
			var config = new AmazonS3Config();
			if (region != null)
				config.RegionEndpoint = region.ToRegionEndpoint();
			if (serviceUrl != null)
				config.ServiceURL = serviceUrl;
			return config;
		}

		/// <summary>
		/// Creates a new instance of <see cref="AwsS3BlobStorage"/> for a given S3 client configuration
		/// </summary>
		public AwsS3BlobStorage(string accessKeyId, string secretAccessKey, string sessionToken,
		   string bucketName, AmazonS3Config clientConfig)
		   : this(accessKeyId, secretAccessKey, sessionToken, bucketName, clientConfig, new TransferUtilityConfig()) {
		}

		/// <summary>
		/// Creates a new instance of <see cref="AwsS3BlobStorage"/> for a given S3 client configuration
		/// </summary>
		public AwsS3BlobStorage(string accessKeyId, string secretAccessKey, string sessionToken,
		   string bucketName, AmazonS3Config clientConfig, TransferUtilityConfig transferUtilityConfig) {
			if (accessKeyId == null)
				throw new ArgumentNullException(nameof(accessKeyId));
			if (secretAccessKey == null)
				throw new ArgumentNullException(nameof(secretAccessKey));
			_bucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));

			AWSCredentials awsCreds = (sessionToken == null)
			   ? (AWSCredentials)new BasicAWSCredentials(accessKeyId, secretAccessKey)
			   : new SessionAWSCredentials(accessKeyId, secretAccessKey, sessionToken);

			_client = new AmazonS3Client(awsCreds, clientConfig);

			_fileTransferUtility = new TransferUtility(_client, transferUtilityConfig ?? new TransferUtilityConfig());
		}

		/// <summary>
		/// Create a client and ensure the S3 bucket exists
		/// </summary>
		private async Task<AmazonS3Client> GetClientAsync() {
			if (!_initialised) {
				var bucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(_client, _bucketName);
				if (!bucketExists) {
					var request = new PutBucketRequest { BucketName = _bucketName };

					await _client.PutBucketAsync(request).ConfigureAwait(false);
				}

				_initialised = true;
			}

			return _client;
		}

		/// <summary>
		/// Lists all buckets, optionaly filtering by prefix. Prefix filtering happens on client side.
		/// </summary>
		public async Task<IReadOnlyCollection<Blob>> ListAsync(ListOptions options = null, CancellationToken cancellationToken = default) {
			if (options == null)
				options = new ListOptions();

			GenericValidation.CheckBlobPrefix(options.FilePrefix);

			AmazonS3Client client = await GetClientAsync().ConfigureAwait(false);

			IReadOnlyCollection<Blob> blobs;
			using (var browser = new AwsS3DirectoryBrowser(client, _bucketName)) {
				blobs = await browser.ListAsync(options, cancellationToken).ConfigureAwait(false);
			}

			if (options.IncludeAttributes) {

				// added null check here to avoid intermittent exceptions when querying for metadata

				foreach (IEnumerable<Blob> page in blobs.Where(b => b != null && !b.IsFolder).Chunk(ListChunkSize)) {
					await Converter.AppendMetadataAsync(client, _bucketName, page, cancellationToken).ConfigureAwait(false);
				}
			}

			return blobs;
		}

		/// <summary>
		/// Uploads a blob to S3 or S3-compatible storage, by automatically computing the Content-Type.
		///
		/// If the supplied stream is not seekable or its length cannot be determined,
		/// the AWS SDK may buffer the entire stream into a `MemoryStream`
		/// before uploading, potentially consuming a large amount of memory.
		/// 
		/// </summary>
		public async Task WriteAsync(string fullPath, Stream dataStream, bool append = false,
		   CancellationToken cancellationToken = default) {
			await WriteAsync(fullPath, dataStream, null, append, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Uploads a blob to S3 or S3-compatible storage, with the given Content-Type.
		///
		/// Uses `TransferUtility` API for AWS S3, MinIO, Wasabi, DigitalOcean Spaces.
		/// Uses `PutObjectAsync` API for Cloudflare R2.
		/// `TransferUtility` performs either a single PUT or a multipart upload depending on the stream size.
		///
		/// If the supplied stream is not seekable or its length cannot be determined,
		/// the AWS SDK may buffer the entire stream into a `MemoryStream`
		/// before uploading, potentially consuming a large amount of memory.
		/// 
		/// </summary>
		public async Task WriteAsync(string fullPath, Stream dataStream, string contentType,
			bool append = false, CancellationToken cancellationToken = default) {

			if (append)
				throw new NotSupportedException();

			// Compute the full object path.
			GenericValidation.CheckBlobFullPath(fullPath);
			fullPath = StoragePath.Normalize(fullPath, true);

			// Auto compute a MIME type (content type) if not given
			if (contentType == null) {
				contentType = MimeUtility.GetMimeMapping(fullPath);
			}

			// if PutObject API is required
			if (_usePutObject) {

				// Use PutObjectAsync for Cloudflare R2.
				var request = new PutObjectRequest {
					BucketName = _bucketName,
					Key = fullPath,
					InputStream = dataStream,
					ContentType = contentType,
					DisablePayloadSigning = _disablePayloadSigning // R2 does not support "Streaming Signature V4".
				};

				await _client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);

			}
			else {

				// Use TransferUtility for AWS S3, MinIO, Wasabi, DigitalOcean Spaces.
				var request = new TransferUtilityUploadRequest {
					BucketName = _bucketName,
					Key = fullPath,
					InputStream = dataStream,
					ContentType = contentType
				};

				await _fileTransferUtility.UploadAsync(request, cancellationToken).ConfigureAwait(false);

			}
		}

		/// <summary>
		/// Opens a blob for reading and returns its content stream.
		///
		/// The returned stream wraps the AWS response stream and must be disposed by the caller,
		/// which also disposes the underlying HTTP response.
		/// Returns <c>null</c> if the blob does not exist.
		/// </summary>
		public async Task<Stream> OpenReadAsync(string fullPath, CancellationToken cancellationToken = default) {
			GenericValidation.CheckBlobFullPath(fullPath);

			fullPath = StoragePath.Normalize(fullPath, true);
			GetObjectResponse response = await GetObjectAsync(fullPath).ConfigureAwait(false);
			if (response == null)
				return null;

			return new FixedStream(response.ResponseStream, length: response.ContentLength, (Action<FixedStream>)null);
		}

		/// <summary>
		/// Deletes multiple blobs in parallel.
		///
		/// Each path is processed independently, including deletion of any virtual directory
		/// placeholders beneath the blob's path.
		/// </summary>
		public async Task DeleteAsync(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			AmazonS3Client client = await GetClientAsync().ConfigureAwait(false);

			await Task.WhenAll(fullPaths.Select(fullPath => DeleteAsync(fullPath, client, cancellationToken))).ConfigureAwait(false);
		}

		/// <summary>
		/// Deletes a blob and recursively removes any virtual directory placeholder objects
		/// beneath its path.
		///
		/// S3 has no real directories; this cleans up any objects that emulate them.
		/// </summary>
		private async Task DeleteAsync(string fullPath, AmazonS3Client client, CancellationToken cancellationToken = default) {
			GenericValidation.CheckBlobFullPath(fullPath);

			fullPath = StoragePath.Normalize(fullPath, true);

			await client.DeleteObjectAsync(_bucketName, fullPath, cancellationToken).ConfigureAwait(false);
			using (var browser = new AwsS3DirectoryBrowser(client, _bucketName)) {
				await browser.DeleteRecursiveAsync(fullPath, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Determines whether each specified blob exists.
		///
		/// The existence checks are performed in parallel.
		/// </summary>
		public async Task<IReadOnlyCollection<bool>> ExistsAsync(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			AmazonS3Client client = await GetClientAsync().ConfigureAwait(false);

			return await Task.WhenAll(fullPaths.Select(fullPath => ExistsAsync(client, fullPath, cancellationToken))).ConfigureAwait(false);
		}

		/// <summary>
		/// Determines whether a blob exists by requesting its metadata.
		///
		/// Returns <c>false</c> if the object is not found.
		/// </summary>
		private async Task<bool> ExistsAsync(AmazonS3Client client, string fullPath, CancellationToken cancellationToken) {
			GenericValidation.CheckBlobFullPath(fullPath);

			try {
				fullPath = StoragePath.Normalize(fullPath, true);
				await client.GetObjectMetadataAsync(_bucketName, fullPath, cancellationToken).ConfigureAwait(false);
				return true;
			}
			catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound) {

			}

			return false;
		}

		/// <summary>
		/// Retrieves metadata for multiple blobs in parallel.
		///
		/// Blobs that do not exist are returned as <c>null</c>.
		/// </summary>
		public async Task<IReadOnlyCollection<Blob>> GetBlobsAsync(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			return await Task.WhenAll(fullPaths.Select(GetBlobAsync)).ConfigureAwait(false);
		}

		/// <summary>
		/// Retrieves a blob's metadata without downloading its contents.
		///
		/// Returns <c>null</c> if the blob does not exist.
		/// </summary>
		private async Task<Blob> GetBlobAsync(string fullPath) {
			GenericValidation.CheckBlobFullPath(fullPath);
			fullPath = StoragePath.Normalize(fullPath, true);

			AmazonS3Client client = await GetClientAsync().ConfigureAwait(false);

			try {
				GetObjectMetadataResponse meta = await client.GetObjectMetadataAsync(_bucketName, fullPath).ConfigureAwait(false);
				return meta.ToBlob(fullPath);
			}
			catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
				//if blob is not found, don't return any information
			}

			return null;
		}

		/// <summary>
		/// Updates the metadata for the specified blobs.
		///
		/// S3 metadata is immutable, so each update is implemented by copying the object
		/// onto itself with replacement metadata. Blob contents are not re-uploaded.
		/// Blobs with no metadata are skipped.
		/// </summary>
		public async Task SetBlobsAsync(IEnumerable<Blob> blobs, CancellationToken cancellationToken = default) {
			if (blobs == null)
				return;

			AmazonS3Client client = await GetClientAsync().ConfigureAwait(false);

			foreach (Blob blob in blobs.Where(b => b != null)) {
				if (blob.Metadata != null) {
					await Converter.UpdateMetadataAsync(
					   client,
					   blob,
					   _bucketName,
					   StoragePath.Normalize(blob.FullPath, true)).ConfigureAwait(false);
				}
			}
		}

		/// <summary>
		/// Retrieves an object from S3.
		///
		/// Returns <c>null</c> if the object does not exist; otherwise returns the full
		/// S3 response containing the content stream and object metadata.
		/// </summary>
		private async Task<GetObjectResponse> GetObjectAsync(string key) {
			var request = new GetObjectRequest { BucketName = _bucketName, Key = key };
			AmazonS3Client client = await GetClientAsync().ConfigureAwait(false);

			try {
				GetObjectResponse response = await client.GetObjectAsync(request).ConfigureAwait(false);
				return response;
			}
			catch (AmazonS3Exception ex) {
				if (IsDoesntExist(ex))
					return null;

				TryHandleException(ex);
				throw;
			}
		}


		private static bool TryHandleException(AmazonS3Exception ex) {
			if (IsDoesntExist(ex)) {
				throw new StorageException(ErrorCode.NotFound, ex);
			}

			return false;
		}

		private static bool IsDoesntExist(AmazonS3Exception ex) {
			return ex.ErrorCode == "NoSuchKey";
		}

		public void Dispose() {
		}

		public Task<ITransaction> OpenTransactionAsync() {
			return Task.FromResult(EmptyTransaction.Instance);
		}

		/// <summary>
		/// Get pre-signed URL for upload object to Blob Storage.
		/// </summary>
		public async Task<string> GetUploadUrlAsync(string fullPath, string mimeType, int expiresInSeconds = 86000) {
			return await GetPresignedUrlAsync(fullPath, mimeType, expiresInSeconds, HttpVerb.PUT).ConfigureAwait(false);
		}

		/// <summary>
		/// Get pre-signed URL for download object from Blob Storage.
		/// </summary>
		public async Task<string> GetDownloadUrlAsync(string fullPath, string mimeType, int expiresInSeconds = 86000) {
			return await GetPresignedUrlAsync(fullPath, mimeType, expiresInSeconds, HttpVerb.GET).ConfigureAwait(false);
		}

		/// <summary>
		/// Generates a pre-signed URL for the specified blob.
		/// </summary>
		public async Task<string> GetPresignedUrlAsync(string fullPath, string mimeType, int expiresInSeconds, HttpVerb verb) {
			return await GetPresignedUrlAsync(fullPath, mimeType, expiresInSeconds, verb, default).ConfigureAwait(false);
		}

		/// <summary>
		/// Generates a pre-signed URL for the specified blob.
		///
		/// The URL grants temporary access to the object using the supplied HTTP verb and
		/// expires after the specified duration. When a MIME type is provided, it is included
		/// in the signature and must be supplied by the client when making the request.
		/// </summary>
		public async Task<string> GetPresignedUrlAsync(string fullPath, string mimeType, int expiresInSeconds, HttpVerb verb, Protocol protocol) {
			IAmazonS3 client = await GetClientAsync().ConfigureAwait(false);

			var request = new GetPreSignedUrlRequest() {
				BucketName = _bucketName,
				Expires = DateTime.UtcNow.AddSeconds(expiresInSeconds),
				Key = StoragePath.Normalize(fullPath, true),
				Protocol = protocol,
				Verb = verb,
			};

			// #122 : If `ContentType` is not set, the generated SDK request signature does not include a `Content-Type` header.
			if (!string.IsNullOrWhiteSpace(mimeType))
				request.ContentType = mimeType;

			return await client.GetPreSignedURLAsync(request);
		}

		/// <summary>
		/// Sets the object's canned ACL.
		///
		/// The supplied ACL string must match one of the AWS predefined canned ACL values;
		/// otherwise an <see cref="ArgumentException"/> is thrown.
		/// </summary>
		public async Task SetAcl(string fullPath, string acl) {
			IAmazonS3 client = await GetClientAsync().ConfigureAwait(false);
			var s3CannedAcl = S3CannedACL.FindValue(acl);
			if (s3CannedAcl is null) {
				throw new ArgumentException($"don't know '{acl}' acl", acl);
			}

			await client.PutObjectAclAsync(new PutObjectAclRequest {
				BucketName = _bucketName,
				Key = StoragePath.Normalize(fullPath, true),
				ACL = s3CannedAcl
			});
		}

	}
}
