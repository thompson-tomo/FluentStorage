using FluentStorage.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Amazon.S3;
using Amazon.Runtime;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System.Threading.Tasks;
using System.Threading;
using FluentStorage.Streaming;
using Amazon.S3.Util;
using MimeMapping;
using FluentStorage.AWS.Utils;
using FluentStorage.Enums;
using FluentStorage.Exceptions;
using FluentStorage.Utils.Extensions;

namespace FluentStorage.AWS.Storage {
	/// <summary>
	/// Manages a single S3 or S3-compatible bucket.
	/// </summary>
	public class S3Store : StoreBase, IS3Storage {
		private const int ListChunkSize = 10;
		private readonly string _bucketName;
		private readonly AmazonS3Client _client;
		private readonly TransferUtility _fileTransferUtility;
		private bool _initialised = false;
		private bool _usePutObject = false;
		private bool _disablePayloadSigning = false;


		/// <summary>
		/// Return bucket name.
		/// </summary>
		public string BucketName => _bucketName;

#if !NET16
		public static S3Store FromAwsCliProfile(string profileName, string bucketName, string region) {
			return new S3Store(bucketName, region, AwsCliCredentials.GetCredentials(profileName));
		}
		public static S3Store FromAwsCredentials(AWSCredentials credentials, string bucketName, string region) {
			return new S3Store(bucketName, region, credentials);
		}
		/// <summary>
		/// Creates an S3-compatible blob storage backed by DigitalOcean Spaces.
		/// </summary>
		public static S3Store FromDigitalOcean(string accessKeyId, string secretAccessKey, string bucketName, string digitalOceanRegion, string sessionToken = null) {
			var serviceUrl = $"https://{digitalOceanRegion}.digitaloceanspaces.com";
			return new S3Store(accessKeyId, secretAccessKey, sessionToken, bucketName, null, serviceUrl);
		}
		/// <summary>
		/// Creates an S3-compatible blob storage backed by MinIO.
		/// </summary>
		public static S3Store FromMinIO(string accessKeyId, string secretAccessKey, string bucketName, string awsRegion, string minioServerUrl, string sessionToken = null) {
			var config = new AmazonS3Config {
				AuthenticationRegion = awsRegion,
				ServiceURL = minioServerUrl,
				ForcePathStyle = true,
			};
			return new S3Store(accessKeyId, secretAccessKey, sessionToken, bucketName, config);
		}
		/// <summary>
		/// Creates an S3-compatible blob storage backed by Wasabi.
		/// </summary>
		public static S3Store FromWasabi(string accessKeyId, string secretAccessKey, string bucketName, string wasabiServiceUrl, string sessionToken = null) {
			return new S3Store(accessKeyId, secretAccessKey, sessionToken, bucketName, null, wasabiServiceUrl);
		}
		/// <summary>
		/// Creates an S3-compatible blob storage backed by Cloudflare R2 Storage.
		/// </summary>
		public static S3Store FromCloudflareR2(string accessKeyId, string secretAccessKey, string bucketName, string cloudflareAccountId, string sessionToken = null) {

			var config = new AmazonS3Config {
				// ServiceURL is always https://<account-id>.r2.cloudflarestorage.com.
				ServiceURL = $"https://{cloudflareAccountId}.r2.cloudflarestorage.com",
				// AuthenticationRegion = "auto" is the recommended value for R2
				AuthenticationRegion = "auto",
				// Uses virtual-hosted style requests, which R2 supports and recommends
				ForcePathStyle = false,
				// Ensures HTTPS (the endpoint itself is HTTPS, but this makes it explicit).
				UseHttp = false,
			};

			var store = new S3Store(accessKeyId, secretAccessKey, sessionToken, bucketName, config);
			store._usePutObject = true;
			store._disablePayloadSigning = true;
			return store;
		}
		/// <summary>
		/// Creates an S3-compatible blob storage backed by Backblaze B2.
		/// </summary>
		public static S3Store FromBackblazeB2(string accessKeyId,string secretAccessKey,string bucketName,string region, string sessionToken = null) {

			var config = new AmazonS3Config {
				// Endpoint format is https://s3.<region>.backblazeb2.com
				ServiceURL = $"https://s3.{region}.backblazeb2.com",
				// Requests are signed with the bucket region.
				AuthenticationRegion = region,
				// Uses virtual-hosted style requests, which B2 supports
				ForcePathStyle = false,
				// Ensures HTTPS (the endpoint itself is HTTPS, but this makes it explicit).
				UseHttp = false,
			};

			var store = new S3Store(accessKeyId, secretAccessKey, sessionToken, bucketName, config);
			return store;
		}
		/// <summary>
		/// Creates an S3-compatible blob storage backed by Hetzner Object Storage.
		/// </summary>
		public static S3Store FromHetzner(string accessKeyId,string secretAccessKey,string bucketName,string region, string sessionToken = null) {

			var config = new AmazonS3Config {
				// Endpoint format is https://<region>.your-objectstorage.com
				ServiceURL = $"https://{region}.your-objectstorage.com",
				// Requests are signed with the bucket region.
				AuthenticationRegion = region,
				// Uses virtual-hosted style requests
				ForcePathStyle = false,
				// Ensures HTTPS (the endpoint itself is HTTPS, but this makes it explicit).
				UseHttp = false,
			};

			var store = new S3Store(accessKeyId, secretAccessKey, sessionToken, bucketName, config);
			return store;
		}
		/// <summary>
		/// Creates an S3-compatible blob storage backed by Vultr Object Storage.
		/// </summary>
		public static S3Store FromVultr(string accessKeyId,string secretAccessKey,string bucketName,string hostname, string sessionToken = null) {

			var config = new AmazonS3Config {
				// Endpoint is unique per cluster.
				ServiceURL = $"https://{hostname}",
				// Vultr accepts us-east-1 for request signing.
				AuthenticationRegion = "us-east-1",
				// Uses virtual-hosted style requests
				ForcePathStyle = false,
				// Ensures HTTPS (the endpoint itself is HTTPS, but this makes it explicit).
				UseHttp = false,
			};

			var store = new S3Store(accessKeyId, secretAccessKey, sessionToken, bucketName, config);
			return store;
		}
#endif

		// [ADD STORAGE PROVIDER]


		public S3Store(string bucketName, string region, AWSCredentials credentials) {
			_bucketName = bucketName;
			_client = new AmazonS3Client(credentials, CreateConfig(region, null));
			_fileTransferUtility = new TransferUtility(_client);
		}

		/// <summary>
		/// Creates a new instance of <see cref="S3Store"/> for a given region endpoint, and will assume the running AWS ECS Task role credentials or Lambda role credentials.
		/// </summary>
		public S3Store(string bucketName, string region) {
			_bucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
			_client = new AmazonS3Client(region.ToRegionEndpoint());
			_fileTransferUtility = new TransferUtility(_client);
		}

		/// <summary>
		/// Creates a new instance of <see cref="S3Store"/> for a given region endpoint.
		/// </summary>
		public S3Store(string accessKeyId, string secretAccessKey, string sessionToken, string bucketName, string region, string serviceUrl)
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
		/// Creates a new instance of <see cref="S3Store"/> for a given S3 client configuration
		/// </summary>
		public S3Store(string accessKeyId, string secretAccessKey, string sessionToken,
		   string bucketName, AmazonS3Config clientConfig)
		   : this(accessKeyId, secretAccessKey, sessionToken, bucketName, clientConfig, new TransferUtilityConfig()) {
		}

		/// <summary>
		/// Creates a new instance of <see cref="S3Store"/> for a given S3 client configuration
		/// </summary>
		public S3Store(string accessKeyId, string secretAccessKey, string sessionToken,
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
		/// Returns the AmazonS3Client instance for this store.
		/// </summary>
		public override async Task<object> GetClient() {
			return await Client();
		}

		/// <summary>
		/// Create a client and checks if the S3 bucket exists.
		/// </summary>
		private async Task<AmazonS3Client> Client() {

			if (!_initialised) {
				var bucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(_client, _bucketName);
				if (!bucketExists) {
					throw new StorageException($"Bucket '{_bucketName}' does not exist!");
				}
				else {
					_initialised = true;
				}
			}

			return _client;
		}

		/// <summary>
		/// Lists all buckets, optionaly filtering by prefix. Prefix filtering happens on client side.
		/// </summary>
		public async Task<List<StoreObject>> ListObjects(StorageListOptions options = null, CancellationToken cancellationToken = default) {
			if (options == null)
				options = new StorageListOptions();

			GenericValidation.CheckBlobPrefix(options.FilePrefix);

			AmazonS3Client client = await Client().ConfigureAwait(false);

			List<StoreObject> blobs;
			using (var browser = new S3DirectoryBrowser(client, _bucketName)) {
				blobs = await browser.ListAsync(options, cancellationToken).ConfigureAwait(false);
			}

			if (options.IncludeAttributes) {

				// added null check here to avoid intermittent exceptions when querying for metadata

				foreach (IEnumerable<StoreObject> page in blobs.Where(b => b != null && !b.IsFolder).Chunk(ListChunkSize)) {
					await AwsConverter.AppendMetadataAsync(client, _bucketName, page, cancellationToken).ConfigureAwait(false);
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
		public async Task SetObject(string fullPath, Stream dataStream, bool append = false,
		   CancellationToken cancellationToken = default) {
			await SetObject(fullPath, dataStream, null, append, cancellationToken).ConfigureAwait(false);
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
		public async Task SetObject(string fullPath, Stream dataStream, string contentType,
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
		/// Returns null if the blob does not exist.
		/// </summary>
		public async Task<Stream> OpenRead(string fullPath, CancellationToken cancellationToken = default) {
			GenericValidation.CheckBlobFullPath(fullPath);

			fullPath = StoragePath.Normalize(fullPath, true);
			GetObjectResponse response = await GetObjectAsync(fullPath).ConfigureAwait(false);
			if (response == null)
				return null;

			return new FixedStream(response.ResponseStream, length: response.ContentLength, (Action<FixedStream>)null);
		}

		/// <summary>
		/// Deletes multiple objects in parallel.
		///
		/// Each path is processed independently, including deletion of any virtual directory
		/// placeholders beneath the object's path.
		/// </summary>
		public override async Task DeleteObjects(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			
			await Task.WhenAll(fullPaths.Select(fullPath => DeleteObject(fullPath, cancellationToken))).ConfigureAwait(false);
		}

		/// <summary>
		/// Deletes a blob and recursively removes any virtual directory placeholder objects
		/// beneath its path.
		/// </summary>
		public override async Task DeleteObject(string fullPath, CancellationToken cancellationToken = default) {
			AmazonS3Client client = await Client().ConfigureAwait(false);

			GenericValidation.CheckBlobFullPath(fullPath);

			fullPath = StoragePath.Normalize(fullPath, true);

			await client.DeleteObjectAsync(_bucketName, fullPath, cancellationToken).ConfigureAwait(false);
			using (var browser = new S3DirectoryBrowser(client, _bucketName)) {
				await browser.DeleteRecursiveAsync(fullPath, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Determines whether each specified blob exists.
		///
		/// The existence checks are performed in parallel.
		/// </summary>
		public async Task<List<bool>> ObjectsExists(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			
			return (await Task.WhenAll(fullPaths.Select(fullPath => ObjectExists(fullPath, cancellationToken))).ConfigureAwait(false)).ToList();
		}

		/// <summary>
		/// Determines whether a blob exists by requesting its metadata.
		/// </summary>
		public override async Task<bool> ObjectExists(string fullPath, CancellationToken cancellationToken) {
			GenericValidation.CheckBlobFullPath(fullPath);

			try {
				AmazonS3Client client = await Client().ConfigureAwait(false);
				fullPath = StoragePath.Normalize(fullPath, true);
				await client.GetObjectMetadataAsync(_bucketName, fullPath, cancellationToken).ConfigureAwait(false);
				return true;
			}
			catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound) {

			}

			return false;
		}

		/// <summary>
		/// Retrieves metadata for multiple objects in parallel.
		///
		/// Blobs that do not exist are returned as null.
		/// </summary>
		public override async Task<List<StoreObject>> GetObjectsInfo(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			return (await Task.WhenAll(fullPaths.Select(fullPath => GetObjectInfo(fullPath, cancellationToken))).ConfigureAwait(false)).ToList();
		}

		/// <summary>
		/// Retrieves a object's metadata without downloading its contents.
		///
		/// Returns null if the blob does not exist.
		/// </summary>
		public override async Task<StoreObject> GetObjectInfo(string fullPath, CancellationToken cancellationToken = default) {
			GenericValidation.CheckBlobFullPath(fullPath);
			fullPath = StoragePath.Normalize(fullPath, true);

			AmazonS3Client client = await Client().ConfigureAwait(false);

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
		public override async Task SetObjectsInfo(IEnumerable<StoreObject> blobs, CancellationToken cancellationToken = default) {
			if (blobs == null)
				return;

			AmazonS3Client client = await Client().ConfigureAwait(false);

			foreach (StoreObject blob in blobs.Where(b => b != null)) {
				if (blob.Metadata != null) {
					await AwsConverter.UpdateMetadataAsync(
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
		/// Returns null if the object does not exist; otherwise returns the full
		/// S3 response containing the content stream and object metadata.
		/// </summary>
		private async Task<GetObjectResponse> GetObjectAsync(string key) {
			var request = new GetObjectRequest { BucketName = _bucketName, Key = key };
			AmazonS3Client client = await Client().ConfigureAwait(false);

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
				throw new StorageException(StorageErrorCode.NotFound, ex);
			}

			return false;
		}

		private static bool IsDoesntExist(AmazonS3Exception ex) {
			return ex.ErrorCode == "NoSuchKey";
		}

		/// <summary>
		/// Generates a pre-signed URL for the specified object.
		///
		/// The URL grants temporary access to the object using the supplied HTTP verb and
		/// expires after the specified duration. When a MIME type is provided, it is included
		/// in the signature and must be supplied by the client when making the request.
		/// </summary>
		public override async Task<string> GetPresignedUrl(string fullPath, string mimeType, bool forDownload, bool https, int expiresInSeconds = 86000) {
			IAmazonS3 client = await Client().ConfigureAwait(false);

			var request = new GetPreSignedUrlRequest() {
				BucketName = _bucketName,
				Expires = DateTime.UtcNow.AddSeconds(expiresInSeconds),
				Key = StoragePath.Normalize(fullPath, true),
				Protocol = https ? Protocol.HTTPS : Protocol.HTTP,
				Verb = forDownload ? HttpVerb.GET : HttpVerb.PUT,
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
			IAmazonS3 client = await Client().ConfigureAwait(false);
			var s3CannedAcl = S3CannedACL.FindValue(acl);
			if (s3CannedAcl is null) {
				throw new ArgumentException($"Unknown ACL value '{acl}'", acl);
			}

			await client.PutObjectAclAsync(new PutObjectAclRequest {
				BucketName = _bucketName,
				Key = StoragePath.Normalize(fullPath, true),
				ACL = s3CannedAcl
			});
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

			AmazonS3Client client = await Client().ConfigureAwait(false);

			await client.CopyObjectAsync(new CopyObjectRequest {
				SourceBucket = BucketName,
				SourceKey = oldPath,
				DestinationBucket = BucketName,
				DestinationKey = newPath
			}, cancellationToken).ConfigureAwait(false);

			await client.DeleteObjectAsync(BucketName, oldPath, cancellationToken).ConfigureAwait(false);

			return true;
		}

	}
}
