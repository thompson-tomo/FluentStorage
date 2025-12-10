using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Storage.Blobs;
using FluentStorage.Azure.Blobs;
using Xunit;

namespace FluentStorage.Tests.Integration.Util {
	[Trait("Category", "Blobs")]
	public class MultiRegionStorageTest {
		private const string Account = "testaccount";

		[Theory]
		[InlineData(AzureCloudEnvironment.Global)]
		[InlineData(AzureCloudEnvironment.China)]
		[InlineData(AzureCloudEnvironment.USGovernment)]
		[InlineData(AzureCloudEnvironment.Germany)]
		public void Blob_Factory_methods_use_correct_cloud_endpoint(AzureCloudEnvironment environment) {
			var endpoint = AzureCloudEndpoints.GetBlobEndpoint(environment);

			var expectedHost = $"{Account}.blob.{endpoint}";

			// Use a valid base64 key so StorageSharedKeyCredential does not throw
			string validBase64Key = Convert.ToBase64String(new byte[32]);

			// Shared key
			IAzureBlobStorage blobSharedKey = StorageFactory.Blobs.AzureBlobStorageWithSharedKey(Account, validBase64Key, serviceUri: null, cloudEnvironment: environment);
			var client = GetBlobServiceClient(blobSharedKey);
			Assert.Equal(expectedHost, client.Uri.Host);

			// Token credential
			var tokenCred = new TestTokenCredential();
			IAzureBlobStorage blobToken = StorageFactory.Blobs.AzureBlobStorageWithTokenCredential(Account, tokenCred, environment);
			var client2 = GetBlobServiceClient(blobToken);
			Assert.Equal(expectedHost, client2.Uri.Host);

			// Managed identity
			IAzureBlobStorage blobMsi = StorageFactory.Blobs.AzureBlobStorageWithMsi(Account, clientId: null, azureCloudEnvironment: environment);
			var client3 = GetBlobServiceClient(blobMsi);
			Assert.Equal(expectedHost, client3.Uri.Host);

			// Azure Ad
			IAzureBlobStorage blobAzureAd = StorageFactory.Blobs.AzureBlobStorageWithAzureAd(
				Account,
				tenantId: "test-tenant",
				applicationId: "test-application",
				applicationSecret: "test-secret",
				cloudEnvironment: environment);
			var client4 = GetBlobServiceClient(blobAzureAd);
			Assert.Equal(expectedHost, client4.Uri.Host);
		}

		private static BlobServiceClient GetBlobServiceClient(object storageInstance) {
			ArgumentNullException.ThrowIfNull(storageInstance);

			FieldInfo fi = storageInstance.GetType().GetField("_client", BindingFlags.NonPublic | BindingFlags.Instance) ?? (storageInstance.GetType().BaseType?.GetField("_client", BindingFlags.NonPublic | BindingFlags.Instance));

			if (fi == null) throw new InvalidOperationException("Could not find _client field on storage instance.");

			if (fi.GetValue(storageInstance) is not BlobServiceClient client) throw new InvalidOperationException("_client field is not a BlobServiceClient.");

			return client;
		}

		private class TestTokenCredential : TokenCredential {
			public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) {
				return new AccessToken("fake", DateTimeOffset.UtcNow.AddHours(1));
			}

			public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) {
				return new ValueTask<AccessToken>(new AccessToken("fake", DateTimeOffset.UtcNow.AddHours(1)));
			}
		}
	}
}
