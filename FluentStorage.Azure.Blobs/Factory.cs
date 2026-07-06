using System;
using Azure.Core;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using FluentStorage.ConnectionString;
using FluentStorage.Azure.Blobs;
using FluentStorage.Azure;

namespace FluentStorage {
	/// <summary>
	/// Blob storage factory
	/// </summary>
	public static class Factory {
		/// <summary>
		/// Register Azure module.
		/// </summary>
		public static IModulesFactory UseAzureBlobStorage(this IModulesFactory factory) {
			return factory.Use(new Module());
		}

		/// <summary>
		/// Creates Azure Blob Storage from an existing <see cref="BlobServiceClient"/>.
		/// </summary>
		public static IAzureBlobStorage AzureBlobStorage(this IBlobStorageFactory factory,
		   BlobServiceClient blobServiceClient) {
			return AzureBlobStorage(factory, blobServiceClient, null);
		}

		/// <summary>
		/// Creates Azure Blob Storage from an existing <see cref="BlobServiceClient"/>.
		/// </summary>
		public static IAzureBlobStorage AzureBlobStorage(this IBlobStorageFactory factory,
		   BlobServiceClient blobServiceClient,
		   string containerName) {
			if (blobServiceClient is null) {
				throw new ArgumentNullException(nameof(blobServiceClient));
			}

			return new AzureBlobStorage(blobServiceClient, blobServiceClient.AccountName, containerName: containerName);
		}

		/// <summary>
		/// Connect to local emulator
		/// </summary>
		public static IAzureBlobStorage AzureBlobStorageWithLocalEmulator(this IBlobStorageFactory factory) {
			var credential = new StorageSharedKeyCredential(
			   "devstoreaccount1",
			   "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==");

			var client = new BlobServiceClient(
			   new Uri("http://127.0.0.1:10000/devstoreaccount1"),
			   credential);

			return new AzureBlobStorage(client, "devstoreaccount1", credential);
		}

		/// <summary>
		/// Creates Azure Blob Storage with Shared Key
		/// </summary>
		public static IAzureBlobStorage AzureBlobStorageWithSharedKey(this IBlobStorageFactory factory,
		   string accountName,
		   string key,
		   Uri serviceUri) {
			return AzureBlobStorageWithSharedKey(factory, accountName, key, serviceUri, default);
		}

		/// <summary>
		/// Creates Azure Blob Storage with Shared Key
		/// </summary>
		public static IAzureBlobStorage AzureBlobStorageWithSharedKey(this IBlobStorageFactory factory, string accountName, string key) {
			return AzureBlobStorageWithSharedKey(factory, accountName, key, null, default);
		}

		/// <summary>
		/// Creates Azure Blob Storage with Shared Key
		/// </summary>
		public static IAzureBlobStorage AzureBlobStorageWithSharedKey(this IBlobStorageFactory factory,
		   string accountName,
		   string key,
		   AzureCloudEnvironment cloudEnvironment) {
			return AzureBlobStorageWithSharedKey(factory, accountName, key, null, cloudEnvironment);
		}

		/// <summary>
		/// Creates Azure Blob Storage with Shared Key
		/// </summary>
		public static IAzureBlobStorage AzureBlobStorageWithSharedKey(this IBlobStorageFactory factory,
		   string accountName,
		   string key,
		   Uri serviceUri,
		   AzureCloudEnvironment cloudEnvironment) {
			if (accountName is null)
				throw new ArgumentNullException(nameof(accountName));
			if (key is null)
				throw new ArgumentNullException(nameof(key));

			var credential = new StorageSharedKeyCredential(accountName, key);

			var client = new BlobServiceClient(serviceUri ?? GetServiceUri(accountName, cloudEnvironment), credential);

			return new AzureBlobStorage(client, accountName, credential);
		}

		/// <summary>
		/// Creates Azure Blob Storage with Shared Key
		/// </summary>
		public static IAzureDataLakeStorage AzureDataLakeStorageWithSharedKey(this IBlobStorageFactory factory,
		   string accountName,
		   string key,
		   Uri serviceUri) {
			return AzureDataLakeStorageWithSharedKey(factory, accountName, key, serviceUri, default);
		}

		/// <summary>
		/// Creates Azure DataLake Storage with Shared Key
		/// </summary>
		public static IAzureDataLakeStorage AzureDataLakeStorageWithSharedKey(this IBlobStorageFactory factory, string accountName, string key) {
			return AzureDataLakeStorageWithSharedKey(factory, accountName, key, null, default);
		}

		/// <summary>
		/// Creates Azure DataLake Storage with Shared Key
		/// </summary>
		public static IAzureDataLakeStorage AzureDataLakeStorageWithSharedKey(this IBlobStorageFactory factory,
		   string accountName,
		   string key,
		   AzureCloudEnvironment cloudEnvironment) {
			return AzureDataLakeStorageWithSharedKey(factory, accountName, key, null, cloudEnvironment);
		}

		/// <summary>
		/// Creates Azure DataLake Storage with Shared Key
		/// </summary>
		public static IAzureDataLakeStorage AzureDataLakeStorageWithSharedKey(this IBlobStorageFactory factory,
		   string accountName,
		   string key,
		   Uri serviceUri,
			AzureCloudEnvironment cloudEnvironment) {
			if (accountName is null)
				throw new ArgumentNullException(nameof(accountName));
			if (key is null)
				throw new ArgumentNullException(nameof(key));

			var credential = new StorageSharedKeyCredential(accountName, key);

			var client = new BlobServiceClient(serviceUri ?? GetServiceUri(accountName, cloudEnvironment), credential);

			return new AzureDataLakeStorage(client, accountName, credential, azureCloudEnvironment: cloudEnvironment);
		}

		/// <summary>
		/// Creates Azure Blob Storage with Azure AD
		/// </summary>
		public static IAzureBlobStorage AzureBlobStorageWithAzureAd(this IBlobStorageFactory factory,
		   string accountName,
		   string tenantId,
		   string applicationId,
		   string applicationSecret,
		   AzureCloudEnvironment cloudEnvironment) {
			return AzureBlobStorageWithAzureAd(factory, accountName, tenantId, applicationId, applicationSecret, null, cloudEnvironment);
		}

		/// <summary>
		/// Creates Azure Blob Storage with Azure AD and AD Authority endpoint
		/// </summary>
		public static IAzureBlobStorage AzureBlobStorageWithAzureAd(this IBlobStorageFactory factory,
		   string accountName,
		   string tenantId,
		   string applicationId,
		   string applicationSecret) {
			return AzureBlobStorageWithAzureAd(factory, accountName, tenantId, applicationId, applicationSecret, null, AzureCloudEnvironment.Global);
		}

		/// <summary>
		/// Creates Azure Blob Storage with Azure AD and AD Authority endpoint
		/// </summary>
		public static IAzureBlobStorage AzureBlobStorageWithAzureAd(this IBlobStorageFactory factory,
		   string accountName,
		   string tenantId,
		   string applicationId,
		   string applicationSecret,
		   string activeDirectoryAuthEndpoint) {
			return AzureBlobStorageWithAzureAd(factory, accountName, tenantId, applicationId, applicationSecret, activeDirectoryAuthEndpoint, default);
		}

		/// <summary>
		/// Creates Azure Blob Storage with Azure AD and AD Authority endpoint
		/// </summary>
		public static IAzureBlobStorage AzureBlobStorageWithAzureAd(this IBlobStorageFactory factory,
		   string accountName,
		   string tenantId,
		   string applicationId,
		   string applicationSecret,
		   string activeDirectoryAuthEndpoint,
		   AzureCloudEnvironment cloudEnvironment) {
			if (accountName is null)
				throw new ArgumentNullException(nameof(accountName));
			if (tenantId is null)
				throw new ArgumentNullException(nameof(tenantId));
			if (applicationId is null)
				throw new ArgumentNullException(nameof(applicationId));
			if (applicationSecret is null)
				throw new ArgumentNullException(nameof(applicationSecret));

			TokenCredential credential = AzureStorageIdentity.CreateClientSecretCredential(
				tenantId,
				applicationId,
				applicationSecret,
				activeDirectoryAuthEndpoint,
				cloudEnvironment);

			// Create a client that can authenticate using our token credential
			var client = new BlobServiceClient(GetServiceUri(accountName, cloudEnvironment), credential);

			return new AzureBlobStorage(client, accountName);
		}

		/// <summary>
		/// Creates Azure DataLake Storage with Azure AD
		/// </summary>
		public static IAzureDataLakeStorage AzureDataLakeStorageWithAzureAd(this IBlobStorageFactory factory,
		   string accountName,
		   string tenantId,
		   string applicationId,
		   string applicationSecret,
		   AzureCloudEnvironment cloudEnvironment) {
			return AzureDataLakeStorageWithAzureAd(factory, accountName, tenantId, applicationId, applicationSecret, null, cloudEnvironment);
		}

		/// <summary>
		/// Creates Azure DataLake Storage with Azure AD and AD Authority endpoint
		/// </summary>
		public static IAzureDataLakeStorage AzureDataLakeStorageWithAzureAd(this IBlobStorageFactory factory,
		   string accountName,
		   string tenantId,
		   string applicationId,
		   string applicationSecret) {
			return AzureDataLakeStorageWithAzureAd(factory, accountName, tenantId, applicationId, applicationSecret, null, AzureCloudEnvironment.Global);
		}

		/// <summary>
		/// Creates Azure DataLake Storage with Azure AD and AD Authority endpoint
		/// </summary>
		public static IAzureDataLakeStorage AzureDataLakeStorageWithAzureAd(this IBlobStorageFactory factory,
		   string accountName,
		   string tenantId,
		   string applicationId,
		   string applicationSecret,
		   string activeDirectoryAuthEndpoint) {
			return AzureDataLakeStorageWithAzureAd(factory, accountName, tenantId, applicationId, applicationSecret, activeDirectoryAuthEndpoint, default);
		}

		/// <summary>
		/// Create Azure DataLake Gen 2 Storage with Azure AD
		/// </summary>
		public static IAzureDataLakeStorage AzureDataLakeStorageWithAzureAd(this IBlobStorageFactory factory,
		   string accountName,
		   string tenantId,
		   string applicationId,
		   string applicationSecret,
		   string activeDirectoryAuthEndpoint,
		   AzureCloudEnvironment cloudEnvironment) {
			if (accountName is null)
				throw new ArgumentNullException(nameof(accountName));
			if (tenantId is null)
				throw new ArgumentNullException(nameof(tenantId));
			if (applicationId is null)
				throw new ArgumentNullException(nameof(applicationId));
			if (applicationSecret is null)
				throw new ArgumentNullException(nameof(applicationSecret));

			TokenCredential credential = AzureStorageIdentity.CreateClientSecretCredential(
				tenantId,
				applicationId,
				applicationSecret,
				activeDirectoryAuthEndpoint,
				cloudEnvironment);

			// Create a client that can authenticate using our token credential
			var client = new BlobServiceClient(GetServiceUri(accountName, cloudEnvironment), credential);

			return new AzureDataLakeStorage(client, accountName, azureCloudEnvironment: cloudEnvironment);
		}

		/// <summary>
		/// Creates Azure Blob Storage with Token Credential
		/// </summary>
		public static IAzureBlobStorage AzureBlobStorageWithTokenCredential(this IBlobStorageFactory factory,
		   string accountName,
		   TokenCredential tokenCredential) {
			return AzureBlobStorageWithTokenCredential(factory, accountName, tokenCredential, default);
		}

		/// <summary>
		/// Creates Azure Blob Storage with Token Credential
		/// </summary>
		public static IAzureBlobStorage AzureBlobStorageWithTokenCredential(this IBlobStorageFactory factory,
		   string accountName,
		   TokenCredential tokenCredential,
		   AzureCloudEnvironment azureCloudEnvironment) {
			var client = new BlobServiceClient(GetServiceUri(accountName, azureCloudEnvironment), tokenCredential);

			return new AzureBlobStorage(client, accountName);
		}

		/// <summary>
		/// Creates Azure Blob Storage with Token Credential
		/// </summary>
		public static IAzureBlobStorage AzureBlobStorageWithSas(this IBlobStorageFactory factory,
		   string sas, BlobClientOptions options = default) {
			TryParseSasUrl(sas, out string accountName, out string containerName, out string sasQuery);

			var client = new BlobServiceClient(new Uri(sas), options);

			return new AzureBlobStorage(client, accountName, containerName: containerName);
		}

		/// <summary>
		/// Creates Azure Blob Storage with Managed Identity
		/// </summary>
		public static IAzureBlobStorage AzureBlobStorageWithMsi(this IBlobStorageFactory factory,
		   string accountName,
		   AzureCloudEnvironment azureCloudEnvironment) {
			return AzureBlobStorageWithMsi(factory, accountName, null, azureCloudEnvironment);
		}

		/// <summary>
		/// Creates Azure Blob Storage with Managed Identity
		/// </summary>
		public static IAzureBlobStorage AzureBlobStorageWithMsi(this IBlobStorageFactory factory, string accountName) {
			return AzureBlobStorageWithMsi(factory, accountName, null, default);
		}

		/// <summary>
		/// Creates Azure Blob Storage with Managed Identity (client id)
		/// </summary>
		public static IAzureBlobStorage AzureBlobStorageWithMsi(this IBlobStorageFactory factory,
		   string accountName,
		   string clientId) {
			return AzureBlobStorageWithMsi(factory, accountName, clientId, default);
		}

		/// <summary>
		/// Creates Azure Blob Storage with Managed Identity
		/// </summary>
		public static IAzureBlobStorage AzureBlobStorageWithMsi(this IBlobStorageFactory factory,
		   string accountName,
		   string clientId,
		   AzureCloudEnvironment azureCloudEnvironment) {
			TokenCredential credential = AzureStorageIdentity.CreateManagedIdentityCredential(clientId);

			var client = new BlobServiceClient(GetServiceUri(accountName, azureCloudEnvironment), credential);

			return new AzureBlobStorage(client, accountName);
		}

		/// <summary>
		/// Creates Azure Data Lake Gen 2 with Managed Identity
		/// </summary>
		public static IAzureDataLakeStorage AzureDataLakeStorageWithMsi(this IBlobStorageFactory factory,
		   string accountName,
		   AzureCloudEnvironment azureCloudEnvironment) {
			return AzureDataLakeStorageWithMsi(factory, accountName, null, azureCloudEnvironment);
		}

		/// <summary>
		/// Creates Azure Data Lake Gen 2 with Managed Identity
		/// </summary>
		public static IAzureDataLakeStorage AzureDataLakeStorageWithMsi(this IBlobStorageFactory factory, string accountName) {
			return AzureDataLakeStorageWithMsi(factory, accountName, null, default);
		}

		/// <summary>
		/// Creates Azure Data Lake Gen 2 with Managed Identity (client id)
		/// </summary>
		public static IAzureDataLakeStorage AzureDataLakeStorageWithMsi(this IBlobStorageFactory factory,
		   string accountName,
		   string clientId) {
			return AzureDataLakeStorageWithMsi(factory, accountName, clientId, default);
		}

		/// <summary>
		/// Creates Azure Data Lake Gen 2 Storage with Managed Identity
		/// </summary>
		public static IAzureDataLakeStorage AzureDataLakeStorageWithMsi(this IBlobStorageFactory factory,
		   string accountName,
		   string clientId,
		   AzureCloudEnvironment azureCloudEnvironment) {
			TokenCredential credential = AzureStorageIdentity.CreateManagedIdentityCredential(clientId);

			var client = new BlobServiceClient(GetServiceUri(accountName, azureCloudEnvironment), credential);

			return new AzureDataLakeStorage(client, accountName, azureCloudEnvironment: azureCloudEnvironment);
		}


		/// <summary>
		/// Create connection string for azure blob storage
		/// </summary>
		public static StorageConnectionString ForAzureBlobStorageWithSharedKey(this IConnectionStringFactory factory,
		   string accountName,
		   string accountKey) {
			var cs = new StorageConnectionString(KnownPrefix.AzureBlobStorage);
			cs.Parameters[KnownParameter.AccountName] = accountName;
			cs.Parameters[KnownParameter.KeyOrPassword] = accountKey;
			return cs;
		}

		/// <summary>
		/// Create connection string for Azure DataLake with Shared Key
		/// </summary>
		public static StorageConnectionString ForAzureDataLakeStorageWithSharedKey(this IConnectionStringFactory factory,
		   string accountName,
		   string accountKey) {
			var cs = new StorageConnectionString(KnownPrefix.AzureDataLakeGen2);
			cs.Parameters[KnownParameter.AccountName] = accountName;
			cs.Parameters[KnownParameter.KeyOrPassword] = accountKey;
			return cs;
		}

		/// <summary>
		/// Create connection string for Azure Blob with Azure AD
		/// </summary>
		public static StorageConnectionString ForAzureBlobStorageWithAzureAd(this IConnectionStringFactory factory,
		   string accountName,
		   string tenantId,
		   string applicationId,
		   string applicationSecret) {
			var cs = new StorageConnectionString(KnownPrefix.AzureBlobStorage);
			cs.Parameters[KnownParameter.AccountName] = accountName;
			cs.Parameters[KnownParameter.TenantId] = tenantId;
			cs.Parameters[KnownParameter.ClientId] = applicationId;
			cs.Parameters[KnownParameter.ClientSecret] = applicationSecret;
			return cs;
		}

		/// <summary>
		/// Create connection string for Azure DataLake with Azure AD
		/// </summary>
		public static StorageConnectionString ForAzureDataLakeStorageWithAzureAd(this IConnectionStringFactory factory,
		   string accountName,
		   string tenantId,
		   string applicationId,
		   string applicationSecret) {
			var cs = new StorageConnectionString(KnownPrefix.AzureDataLakeGen2);
			cs.Parameters[KnownParameter.AccountName] = accountName;
			cs.Parameters[KnownParameter.TenantId] = tenantId;
			cs.Parameters[KnownParameter.ClientId] = applicationId;
			cs.Parameters[KnownParameter.ClientSecret] = applicationSecret;
			return cs;
		}

		private static Uri GetServiceUri(string accountName, AzureCloudEnvironment cloudEnvironment = default) {
			return AzureStorageIdentity.CreateBlobServiceUri(accountName, cloudEnvironment);
		}

		private static bool TryParseSasUrl(string url, out string accountName, out string containerName, out string sas) {
			try {
				var u = new Uri(url);

				accountName = u.Host.Substring(0, u.Host.IndexOf('.'));
				containerName = u.Segments.Length == 2 ? u.Segments[1] : null;
				sas = u.Query;

				return true;
			}
			catch {
				accountName = null;
				containerName = null;
				sas = null;
				return false;
			}

		}
	}
}
