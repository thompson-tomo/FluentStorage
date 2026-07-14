using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Api.Gax;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using FluentStorage.Storage;
using GObjects = Google.Apis.Storage.v1.Data.Objects;
using GObject = Google.Apis.Storage.v1.Data.Object;
using Google;
using System.Net;
using System.Linq;
using Google.Apis.Storage.v1;
using FluentStorage.Enums;
using FluentStorage.Streaming;
using FluentStorage.Utils.Validation;

namespace FluentStorage.GCP.Storage {
	/// <summary>
	/// Manages a single Google Cloud Storage bucket.
	/// </summary>
	public class GoogleCloudStore : StoreBase {
		
		private readonly StorageClient _client;
		private readonly string _bucketName;

		public GoogleCloudStore(string bucketName, GoogleCredential credential = null, EncryptionKey encryptionKey = null) : base() {
			_client = StorageClient.Create(credential, encryptionKey);
			_bucketName = bucketName;
		}

		/// <summary>
		/// Returns the StorageClient instance for this store.
		/// </summary>
		public override async Task<object> GetClient() {
			return _client;
		}

		protected override async Task<List<StoreObject>> ListPath(
		   string path, StorageListOptions options, CancellationToken cancellationToken) {

			ObjectsResource.ListRequest request = _client.Service.Objects.List(_bucketName);
			request.Prefix = StoragePath.IsRootPath(path) ? null : (NormalisePath(path) + "/");
			request.Delimiter = "/";
			request.MaxResults = options.PageSize ?? StorageListOptions.PAGE_SIZE;
			
			var page = new List<StoreObject>();
			do {
				GObjects serviceObjects = await request.ExecuteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

				if (serviceObjects.Items != null) {
					page.AddRange(GConvert.ToBlobs(serviceObjects.Items, options));
				}

				if (serviceObjects.Prefixes != null) {
					//the only info we have about prefixes is it's name
					page.AddRange(serviceObjects.Prefixes.Select(p => new StoreObject(p, StorageObjectType.Folder)));
				}


				request.PageToken = serviceObjects.NextPageToken;
			}
			while (request.PageToken != null);

			return page;
		}

		public override async Task SetObjectsInfo(IEnumerable<StoreObject> blobs, CancellationToken cancellationToken = default) {
			ArgValidator.AssertFullPaths(blobs);

			await Task.WhenAll(blobs.Select(b => SetObjectInfo(b, cancellationToken))).ConfigureAwait(false);
		}

		public override async Task SetObjectInfo(StoreObject blob, CancellationToken cancellationToken = default) {
			GObject item = await _client.GetObjectAsync(_bucketName, NormalisePath(blob.FullPath), cancellationToken: cancellationToken).ConfigureAwait(false);

			if (item.Metadata == null) {
				item.Metadata = new Dictionary<string, string>();
			}

			foreach (KeyValuePair<string, string> metadata in blob.Metadata) {
				if (item.Metadata.ContainsKey(metadata.Key)) {
					item.Metadata[metadata.Key] = metadata.Value;
				}
				else {
					item.Metadata.Add(metadata.Key, metadata.Value);
				}
			}

			await _client.UpdateObjectAsync(item, cancellationToken: cancellationToken).ConfigureAwait(false);
		}

		public override async Task<StoreObject> GetObjectInfo(string fullPath, CancellationToken cancellationToken) {
			fullPath = NormalisePath(fullPath);

			try {
				GObject obj = await _client.GetObjectAsync(_bucketName, fullPath,
				   new GetObjectOptions {
					   //todo
				   },
				   cancellationToken).ConfigureAwait(false);

				return GConvert.ToBlob(obj);
			}
			catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound) {
				return null;
			}
		}

		public override async Task DeleteObject(string fullPath, CancellationToken cancellationToken) {
			try {
				await _client.DeleteObjectAsync(_bucketName, NormalisePath(fullPath), cancellationToken: cancellationToken).ConfigureAwait(false);
			}
			catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound) {
				//when not found, just ignore

				//try delete everything recursively
				List<StoreObject?> childObjects = await ListPath(fullPath, new StorageListOptions { Recurse = true }, cancellationToken).ConfigureAwait(false);

				foreach (StoreObject? blob in childObjects) {
					if (blob == null) {
						continue;
					}
					
					try {
						await _client.DeleteObjectAsync(_bucketName, NormalisePath(blob.FullPath), cancellationToken: cancellationToken).ConfigureAwait(false);
					}
					catch (GoogleApiException exc) when (exc.HttpStatusCode == HttpStatusCode.NotFound) {

					}
				}
			}
		}

		public override async Task<bool> ObjectExists(string fullPath, CancellationToken cancellationToken) {
			ArgValidator.AssertFullPath(fullPath);

			try {
				await _client.GetObjectAsync(
				   _bucketName, NormalisePath(fullPath),
				   null,
				   cancellationToken).ConfigureAwait(false);

				return true;
			}
			catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound) {
				return false;
			}
		}


		public override async Task SetObject(string fullPath, Stream dataStream,
		   bool append = false, CancellationToken cancellationToken = default) {
			if (append)
				throw new NotSupportedException();
			ArgValidator.AssertFullPath(fullPath);
			fullPath = NormalisePath(fullPath);

			await _client.UploadObjectAsync(_bucketName, fullPath, null, dataStream, cancellationToken: cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Opens an object for reading and returns its content stream.
		/// </summary>
		public override async Task<Stream> OpenRead(string fullPath, CancellationToken cancellationToken = default) {
			ArgValidator.AssertFullPath(fullPath);
			fullPath = NormalisePath(fullPath);

			// no read streaming support in this crappy SDK

			var ms = new MemoryStream();
			try {
				await _client.DownloadObjectAsync(_bucketName, fullPath, ms, cancellationToken: cancellationToken).ConfigureAwait(false);
			}
			catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound) {
				return null;
			}
			ms.Position = 0;
			return ms;
		}

		/// <summary>
		/// Opens an object for writing and returns its content stream.
		/// Object will be written when the stream is disposed.
		/// </summary>
		public override async Task<Stream> OpenWrite(string fullPath, bool overwrite, CancellationToken cancellationToken = default) {
			ArgValidator.AssertFullPath(fullPath);

			// exit if file exists and overwriting is disabled
			if (!overwrite && await ObjectExists(fullPath, cancellationToken)) return null;

			fullPath = NormalisePath(fullPath);

			MemoryStream stream = new();

			return new FixedStream(stream, null, async s => {

				// write object on stream dispose
				s.Position = 0;

				await _client.UploadObjectAsync(_bucketName,fullPath,
					null,s, null, cancellationToken).ConfigureAwait(false);
			});
		}

		/// <summary>
		/// GCP requires no trailing root
		/// </summary>
		private static string NormalisePath(string path) {
			path = StoragePath.Normalize(path);
			return path.Substring(1);
		}
	}
}
