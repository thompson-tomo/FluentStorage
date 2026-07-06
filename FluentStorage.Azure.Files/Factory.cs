using System;
using Azure.Core;
using Azure.Storage;
using Azure.Storage.Files.Shares;
using FluentStorage.Azure.Files;
using FluentStorage.Azure;
using FluentStorage.Blobs;
using FluentStorage.ConnectionString;

namespace FluentStorage {
	/// <summary>
	/// Azure Files/DataLake Factory that is accessible using `FluentStorage.StorageFactory.Blobs` by way of extension methods.
	/// </summary>
	public static class AzureFilesFactory {
		/// <summary>
		/// Register Azure module.
		/// </summary>
		public static IModulesFactory UseAzureFilesStorage(this IModulesFactory factory) {
			return factory.Use(new Module());
		}

		/// <summary>
		/// Creates Azure Files from an existing <see cref="ShareServiceClient"/>.
		/// </summary>
		public static IBlobStorage AzureFiles(this IBlobStorageFactory factory,
		   ShareServiceClient shareServiceClient) {
			if (shareServiceClient is null) {
				throw new ArgumentNullException(nameof(shareServiceClient));
			}

			return new AzureFilesBlobStorage(shareServiceClient, shareServiceClient.AccountName);
		}

		/// <summary>
		/// Creates Azure Files from account name and key
		/// </summary>
		/// <param name="factory">Reference to factory</param>
		/// <param name="accountName">Storage Account name</param>
		/// <param name="key">Storage Account key</param>
		/// <returns>Generic blob storage interface</returns>
		public static IBlobStorage AzureFiles(this IBlobStorageFactory factory,
		   string accountName,
		   string key) {
			return AzureFilesWithSharedKey(factory, accountName, key);
		}

		/// <summary>
		/// Creates Azure Files with Shared Key
		/// </summary>
		public static IBlobStorage AzureFilesWithSharedKey(this IBlobStorageFactory factory,
		   string accountName,
		   string key,
		   Uri serviceUri) {
			return AzureFilesWithSharedKey(factory, accountName, key, serviceUri, default);
		}

		/// <summary>
		///Creates Azure Files with Shared Key
		/// </summary>
		public static IBlobStorage AzureFilesWithSharedKey(this IBlobStorageFactory factory,
		   string accountName,
		   string key) {
			return AzureFilesWithSharedKey(factory, accountName, key, null, default);
		}

		/// <summary>
		///Creates Azure Files with Shared Key
		/// </summary>
		public static IBlobStorage AzureFilesWithSharedKey(this IBlobStorageFactory factory,
		   string accountName,
		   string key,
		   AzureCloudEnvironment cloudEnvironment) {
			return AzureFilesWithSharedKey(factory, accountName, key, null, cloudEnvironment);
		}

		/// <summary>
		///Creates Azure Files with Shared Key
		/// </summary>
		public static IBlobStorage AzureFilesWithSharedKey(this IBlobStorageFactory factory,
		   string accountName,
		   string key,
		   Uri serviceUri,
		   AzureCloudEnvironment cloudEnvironment) {
			if (accountName is null) {
				throw new ArgumentNullException(nameof(accountName));
			}
			if (key is null) {
				throw new ArgumentNullException(nameof(key));
			}

			var credential = new StorageSharedKeyCredential(accountName, key);
			var client = new ShareServiceClient(serviceUri ?? GetServiceUri(accountName, cloudEnvironment), credential);

			return new AzureFilesBlobStorage(client, accountName);
		}

		/// <summary>
		/// Create Azure Files with Azure AD 
		/// </summary>
		public static IBlobStorage AzureFilesWithAzureAd(this IBlobStorageFactory factory,
		   string accountName,
		   string tenantId,
		   string applicationId,
		   string applicationSecret,
		   AzureCloudEnvironment cloudEnvironment) {
			return AzureFilesWithAzureAd(factory, accountName, tenantId, applicationId, applicationSecret, null, cloudEnvironment);
		}

		/// <summary>
		/// Create Azure Files with Azure AD and Active Directory Authority endpoint.
		/// </summary>
		public static IBlobStorage AzureFilesWithAzureAd(this IBlobStorageFactory factory,
		   string accountName,
		   string tenantId,
		   string applicationId,
		   string applicationSecret) {
			return AzureFilesWithAzureAd(factory, accountName, tenantId, applicationId, applicationSecret, null, AzureCloudEnvironment.Global);
		}

		/// <summary>
		/// Create Azure Files with Azure AD and Active Directory Authority endpoint.
		/// </summary>
		public static IBlobStorage AzureFilesWithAzureAd(this IBlobStorageFactory factory,
		   string accountName,
		   string tenantId,
		   string applicationId,
		   string applicationSecret,
		   string activeDirectoryAuthEndpoint) {
			return AzureFilesWithAzureAd(factory, accountName, tenantId, applicationId, applicationSecret, activeDirectoryAuthEndpoint, default);
		}

		/// <summary>
		/// Create Azure Files with Azure AD and Active Directory Authority endpoint.
		/// </summary>
		public static IBlobStorage AzureFilesWithAzureAd(this IBlobStorageFactory factory,
		   string accountName,
		   string tenantId,
		   string applicationId,
		   string applicationSecret,
		   string activeDirectoryAuthEndpoint,
		   AzureCloudEnvironment cloudEnvironment) {
			if (accountName is null) {
				throw new ArgumentNullException(nameof(accountName));
			}
			if (tenantId is null) {
				throw new ArgumentNullException(nameof(tenantId));
			}
			if (applicationId is null) {
				throw new ArgumentNullException(nameof(applicationId));
			}
			if (applicationSecret is null) {
				throw new ArgumentNullException(nameof(applicationSecret));
			}

			TokenCredential credential = AzureStorageIdentity.CreateClientSecretCredential(
				tenantId,
				applicationId,
				applicationSecret,
				activeDirectoryAuthEndpoint,
				cloudEnvironment);

			var client = new ShareServiceClient(GetServiceUri(accountName, cloudEnvironment), credential);

			return new AzureFilesBlobStorage(client, accountName);
		}

		/// <summary>
		/// Create Azure Files with Token Credentials
		/// </summary>
		public static IBlobStorage AzureFilesWithTokenCredential(this IBlobStorageFactory factory,
		   string accountName,
		   TokenCredential tokenCredential) {
			return AzureFilesWithTokenCredential(factory, accountName, tokenCredential, default);
		}

		/// <summary>
		///Create Azure Files with Token Credentials
		/// </summary>
		public static IBlobStorage AzureFilesWithTokenCredential(this IBlobStorageFactory factory,
		   string accountName,
		   TokenCredential tokenCredential,
		   AzureCloudEnvironment azureCloudEnvironment) {
			if (accountName is null) {
				throw new ArgumentNullException(nameof(accountName));
			}
			if (tokenCredential is null) {
				throw new ArgumentNullException(nameof(tokenCredential));
			}

			var client = new ShareServiceClient(GetServiceUri(accountName, azureCloudEnvironment), tokenCredential);

			return new AzureFilesBlobStorage(client, accountName);
		}

		/// <summary>
		/// Creates Azure Files with Managed Identity (Managed Service Identity)
		/// </summary>
		public static IBlobStorage AzureFilesWithMsi(this IBlobStorageFactory factory,
		   string accountName,
		   AzureCloudEnvironment azureCloudEnvironment) {
			return AzureFilesWithMsi(factory, accountName, null, azureCloudEnvironment);
		}

		/// <summary>
		/// Creates Azure Files with Managed Identity (Managed Service Identity)
		/// </summary>
		public static IBlobStorage AzureFilesWithMsi(this IBlobStorageFactory factory, string accountName) {
			return AzureFilesWithMsi(factory, accountName, null, default);
		}

		/// <summary>
		/// Creates Azure Files with Managed Identity (Managed Service Identity)
		/// </summary>
		public static IBlobStorage AzureFilesWithMsi(this IBlobStorageFactory factory,
		   string accountName,
		   string clientId) {
			return AzureFilesWithMsi(factory, accountName, clientId, default);
		}

		/// <summary>
		/// Creates Azure Files with Managed Identity (Managed Service Identity)
		/// </summary>
		public static IBlobStorage AzureFilesWithMsi(this IBlobStorageFactory factory,
		   string accountName,
		   string clientId,
		   AzureCloudEnvironment azureCloudEnvironment) {
			if (accountName is null) {
				throw new ArgumentNullException(nameof(accountName));
			}

			TokenCredential credential = AzureStorageIdentity.CreateManagedIdentityCredential(clientId);

			var client = new ShareServiceClient(GetServiceUri(accountName, azureCloudEnvironment), credential);

			return new AzureFilesBlobStorage(client, accountName);
		}

		/// <summary>
		/// Create connection string for Azure Files with Shared Key
		/// </summary>
		public static StorageConnectionString ForAzureFilesStorageWithSharedKey(this IConnectionStringFactory factory,
		   string accountName,
		   string accountKey) {
			var cs = new StorageConnectionString(KnownPrefix.AzureFilesStorage);
			cs.Parameters[KnownParameter.AccountName] = accountName;
			cs.Parameters[KnownParameter.KeyOrPassword] = accountKey;
			return cs;
		}

		/// <summary>
		/// Create connection string for Azure Files with Azure AD
		/// </summary>
		public static StorageConnectionString ForAzureFilesStorageWithAzureAd(this IConnectionStringFactory factory,
		   string accountName,
		   string tenantId,
		   string applicationId,
		   string applicationSecret) {
			var cs = new StorageConnectionString(KnownPrefix.AzureFilesStorage);
			cs.Parameters[KnownParameter.AccountName] = accountName;
			cs.Parameters[KnownParameter.TenantId] = tenantId;
			cs.Parameters[KnownParameter.ClientId] = applicationId;
			cs.Parameters[KnownParameter.ClientSecret] = applicationSecret;
			return cs;
		}

		internal static Uri GetServiceUri(string accountName, AzureCloudEnvironment cloudEnvironment = default) {
			return AzureStorageIdentity.CreateFileServiceUri(accountName, cloudEnvironment);
		}
	}
}
