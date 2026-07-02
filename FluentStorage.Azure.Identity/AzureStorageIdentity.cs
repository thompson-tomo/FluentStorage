using System;
using Azure.Core;
using Azure.Identity;

namespace FluentStorage.Azure.Identity {
	/// <summary>
	/// Shared Azure Storage identity helpers.
	/// </summary>
	public static class AzureStorageIdentity {
		/// <summary>
		/// Creates a service principal credential for the specified Azure cloud environment.
		/// </summary>
		public static TokenCredential CreateClientSecretCredential(
		   string tenantId,
		   string applicationId,
		   string applicationSecret,
		   string activeDirectoryAuthEndpoint,
		   AzureCloudEnvironment cloudEnvironment) {
			var authorityHost = activeDirectoryAuthEndpoint is not null
				? new Uri(activeDirectoryAuthEndpoint)
				: AzureCloudEndpoints.GetAuthorityEndpoint(cloudEnvironment);

			return new ClientSecretCredential(
				tenantId,
				applicationId,
				applicationSecret,
				new TokenCredentialOptions { AuthorityHost = authorityHost });
		}

		/// <summary>
		/// Creates a managed identity credential.
		/// </summary>
		public static TokenCredential CreateManagedIdentityCredential(string clientId) {
			return new ManagedIdentityCredential(clientId, null);
		}

		/// <summary>
		/// Creates a Blob service URI for the specified Azure cloud environment.
		/// </summary>
		public static Uri CreateBlobServiceUri(string accountName, AzureCloudEnvironment cloudEnvironment = default) {
			var endpoint = AzureCloudEndpoints.GetBlobEndpoint(cloudEnvironment);
			return new Uri($"https://{accountName}.blob.{endpoint}/");
		}

		/// <summary>
		/// Creates an Azure Files service URI for the specified Azure cloud environment.
		/// </summary>
		public static Uri CreateFileServiceUri(string accountName, AzureCloudEnvironment cloudEnvironment = default) {
			var endpoint = AzureCloudEndpoints.GetBlobEndpoint(cloudEnvironment);
			return new Uri($"https://{accountName}.file.{endpoint}/");
		}
	}
}
