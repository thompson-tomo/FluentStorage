using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using FluentStorage.Storage;
using FluentStorage.Utils.Objects;
using Azure.Core.Pipeline;
using FluentStorage.Azure.Blobs.Storage;
using FluentStorage.Azure.Blobs.DataLake.Model;
using FluentStorage.Azure.Blobs.Utils;

namespace FluentStorage.Azure.Blobs.DataLake {
	public class AzureDataLakeStore : AzureBlobStore, IAzureDataLakeStorage {
		private readonly ExtendedSdk _extended;

		public AzureDataLakeStore(BlobServiceClient client, string accountName, StorageSharedKeyCredential sasSigningCredentials = null, string containerName = null, AzureCloudEnvironment azureCloudEnvironment = default) : base(client, accountName, sasSigningCredentials, containerName) {
			_extended = new ExtendedSdk(client, accountName, azureCloudEnvironment);


			// Fix #41: `ExtendedSdk.GetHttpPipeline` needs to be manually set otherwise connection to DataLake Gen2 fails

			// get `client.ClientConfiguration.Pipeline`
			var config = Reflections.GetProp(client, "_clientConfiguration", false);
			var pipeline = Reflections.GetPropTyped<HttpPipeline>(config, "Pipeline", false);

			// link the pipeline to the client
			Reflections.SetProp(_extended, "_httpPipeline", pipeline);

		}


		public Task<IReadOnlyCollection<Filesystem>> ListFilesystemsAsync(CancellationToken cancellationToken = default) {
			return _extended.ListFilesystemsAsync(cancellationToken);
		}

		public Task CreateFilesystemAsync(string filesystemName, CancellationToken cancellationToken = default) {
			return _extended.CreateFilesystemAsync(filesystemName, cancellationToken);
		}

		public Task DeleteFilesystemAsync(string filesystemName, CancellationToken cancellationToken = default) {
			return _extended.DeleteFilesystemAsync(filesystemName, cancellationToken);
		}

		public Task SetAccessControlAsync(string fullPath, AccessControl accessControl, CancellationToken cancellationToken = default) {
			return _extended.SetAccessControlAsync(fullPath, accessControl, cancellationToken);
		}

		public Task<AccessControl> GetAccessControlAsync(string fullPath, bool getUpn = false, CancellationToken cancellationToken = default) {
			return _extended.GetAccessControlAsync(fullPath, getUpn, cancellationToken);
		}


		protected override Task DeleteAsync(string fullPath, CancellationToken cancellationToken) {
			return _extended.DeleteAsync(fullPath, cancellationToken);
		}

		public override Task<IReadOnlyCollection<StorageObject>> ListAsync(
		   ListOptions options, CancellationToken cancellationToken) {
			return _extended.ListAsync(options, cancellationToken);
		}

		protected override Task<StorageObject> GetBlobAsync(string fullPath, CancellationToken cancellationToken) {
			return _extended.GetBlobAsync(fullPath, cancellationToken);
		}

	}
}
