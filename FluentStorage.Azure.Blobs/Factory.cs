using System;
using Azure.Core;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using FluentStorage.ConnectionString;
using FluentStorage.Azure.Blobs;

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
		///
		/// </summary>
		public static IAzureBlobStorage AzureBlobStorageWithSharedKey(this IBlobStorageFactory factory,
		   string accountName,
		   string key,
		   Uri serviceUri = null,
		   AzureCloudEnvironment cloudEnvironment = default) {
			if (accountName is null)
				throw new ArgumentNullException(nameof(accountName));
			if (key is null)
				throw new ArgumentNullException(nameof(key));

			var credential = new StorageSharedKeyCredential(accountName, key);

			var client = new BlobServiceClient(serviceUri ?? GetServiceUri(accountName, cloudEnvironment), credential);

			return new AzureBlobStorage(client, accountName, credential);
		}

		/// <summary>
		///
		/// </summary>
		public static IAzureDataLakeStorage AzureDataLakeStorageWithSharedKey(this IBlobStorageFactory factory,
		   string accountName,
		   string key,
		   Uri serviceUri = null,
			AzureCloudEnvironment cloudEnvironment = default) {
			if (accountName is null)
				throw new ArgumentNullException(nameof(accountName));
			if (key is null)
				throw new ArgumentNullException(nameof(key));

			var credential = new StorageSharedKeyCredential(accountName, key);

			var client = new BlobServiceClient(serviceUri ?? GetServiceUri(accountName, cloudEnvironment), credential);

			return new AzureDataLakeStorage(client, accountName, credential, azureCloudEnvironment: cloudEnvironment);
		}

		/// <summary>
		/// Overload that accepts a cloud environment.
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
		/// Overload that accepts a custom Active Directory authority endpoint.
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
		/// Canonical implementation (no optional parameters).
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

			var authorityHost = activeDirectoryAuthEndpoint is not null
				? new Uri(activeDirectoryAuthEndpoint)
				: AzureCloudEndpoints.GetAuthorityEndpoint(cloudEnvironment);

			// Create a token credential that can use our Azure Active
			// Directory application to authenticate with Azure Storage
			TokenCredential credential =
				new ClientSecretCredential(
					tenantId,
					applicationId,
					applicationSecret,
					new TokenCredentialOptions { AuthorityHost = authorityHost });

			// Create a client that can authenticate using our token credential
			var client = new BlobServiceClient(GetServiceUri(accountName, cloudEnvironment), credential);

			return new AzureBlobStorage(client, accountName);
		}

		/// <summary>
		/// Overload that accepts a cloud environment.
		/// </summary>
		public static IAzureBlobStorage AzureDataLakeStorageWithAzureAd(this IBlobStorageFactory factory,
		   string accountName,
		   string tenantId,
		   string applicationId,
		   string applicationSecret,
		   AzureCloudEnvironment cloudEnvironment) {
			return AzureDataLakeStorageWithAzureAd(factory, accountName, tenantId, applicationId, applicationSecret, null, cloudEnvironment);
		}

		/// <summary>
		/// Overload that accepts a custom Active Directory authority endpoint.
		/// </summary>
		public static IAzureBlobStorage AzureDataLakeStorageWithAzureAd(this IBlobStorageFactory factory,
		   string accountName,
		   string tenantId,
		   string applicationId,
		   string applicationSecret,
		   string activeDirectoryAuthEndpoint) {
			return AzureDataLakeStorageWithAzureAd(factory, accountName, tenantId, applicationId, applicationSecret, activeDirectoryAuthEndpoint, default);
		}

		/// <summary>
		/// Create Azure Data Lake Gen 2 Storage with AAD authentication
		/// </summary>
		/// <param name="factory"></param>
		/// <param name="accountName"></param>
		/// <param name="tenantId"></param>
		/// <param name="applicationId"></param>
		/// <param name="applicationSecret"></param>
		/// <param name="activeDirectoryAuthEndpoint"></param>
		/// <param name="cloudEnvironment"></param>
		/// <returns></returns>
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

			var authorityHost = activeDirectoryAuthEndpoint is not null
				? new Uri(activeDirectoryAuthEndpoint)
				: AzureCloudEndpoints.GetAuthorityEndpoint(cloudEnvironment);

			// Create a token credential that can use our Azure Active
			// Directory application to authenticate with Azure Storage
			TokenCredential credential =
				new ClientSecretCredential(
					tenantId,
					applicationId,
					applicationSecret,
					new TokenCredentialOptions { AuthorityHost = authorityHost });

			// Create a client that can authenticate using our token credential
			var client = new BlobServiceClient(GetServiceUri(accountName), credential);

			return new AzureDataLakeStorage(client, accountName, azureCloudEnvironment: cloudEnvironment);
		}

		/// <summary>
		///
		/// </summary>
		public static IAzureBlobStorage AzureBlobStorageWithTokenCredential(this IBlobStorageFactory factory,
		   string accountName,
		   TokenCredential tokenCredential,
		   AzureCloudEnvironment azureCloudEnvironment = default) {
			var client = new BlobServiceClient(GetServiceUri(accountName, azureCloudEnvironment), tokenCredential);

			return new AzureBlobStorage(client, accountName);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="factory"></param>
		/// <param name="sas"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public static IAzureBlobStorage AzureBlobStorageWithSas(this IBlobStorageFactory factory,
		   string sas, BlobClientOptions options = default) {
			TryParseSasUrl(sas, out string accountName, out string containerName, out string sasQuery);

			var client = new BlobServiceClient(new Uri(sas), options);

			return new AzureBlobStorage(client, accountName, containerName: containerName);
		}

		/// <summary>
		/// Creates Azure Blob Storage with Managed Identity
		/// </summary>
		/// <param name="factory"></param>
		/// <param name="accountName"></param>
		/// <param name="clientId"></param>
		/// <param name="azureCloudEnvironment"></param>
		/// <returns></returns>
		public static IAzureBlobStorage AzureBlobStorageWithMsi(this IBlobStorageFactory factory,
		   string accountName,
		   string clientId = null,
		   AzureCloudEnvironment azureCloudEnvironment = default) {
			TokenCredential credential = new ManagedIdentityCredential(clientId, null);

			var client = new BlobServiceClient(GetServiceUri(accountName, azureCloudEnvironment), credential);

			return new AzureBlobStorage(client, accountName);
		}



		/// <summary>
		/// Creates Azure Data Lake Gen 2 Storage with Managed Identity
		/// </summary>
		/// <param name="factory"></param>
		/// <param name="accountName"></param>
		/// <param name="clientId"></param>
		/// <param name="azureCloudEnvironment"></param>
		/// <returns></returns>
		public static IAzureDataLakeStorage AzureDataLakeStorageWithMsi(this IBlobStorageFactory factory,
		   string accountName,
		   string clientId = null,
		   AzureCloudEnvironment azureCloudEnvironment = default) {
			TokenCredential credential = new ManagedIdentityCredential(clientId, null);

			var client = new BlobServiceClient(GetServiceUri(accountName, azureCloudEnvironment), credential);

			return new AzureDataLakeStorage(client, accountName, azureCloudEnvironment: azureCloudEnvironment);
		}


		/*
		/// <summary>
		/// Creates a blob storage implementation based on Microsoft Azure Blob Storage using development storage.
		/// </summary>
		/// <param name="factory">Reference to factory</param>
		/// <returns>Generic blob storage interface</returns>
		public static IAzureBlobStorage AzureBlobDevelopmentStorage(this IBlobStorageFactory factory)
		{
		   return AzureUniversalBlobStorageProvider.CreateForLocalEmulator();
		}
		}
		*/

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
		/// Create connection string for azure blob storage
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
		///
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
		///
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
			var endpoint = AzureCloudEndpoints.GetBlobEndpoint(cloudEnvironment);
			return new Uri($"https://{accountName}.blob.{endpoint}/");
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
