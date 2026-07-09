using FluentStorage.Storage;
using FluentStorage.ConnectionString;
using FluentStorage.Queue;

namespace FluentStorage.Azure.Blobs {
	class Module : IExternalModule, IConnectionFactory {
		public IConnectionFactory ConnectionFactory => this;

		public IBucket CreateBlobStorage(StorageConnectionString connectionString) {
			if (connectionString.Prefix == KnownPrefix.AzureBlobStorage) {
				if (connectionString.Parameters.ContainsKey(KnownParameter.IsLocalEmulator)) {
					return AzureBlobStorage.FromLocalEmulator();
				}

				connectionString.GetRequired(KnownParameter.AccountName, true, out string accountName);

				string sharedKey = connectionString.Get(KnownParameter.KeyOrPassword);
				if (!string.IsNullOrEmpty(sharedKey)) {
					return AzureBlobStorage.FromSharedKey(accountName, sharedKey);
				}

				string tenantId = connectionString.Get(KnownParameter.TenantId);
				if (!string.IsNullOrEmpty(tenantId)) {
					connectionString.GetRequired(KnownParameter.ClientId, true, out string clientId);
					connectionString.GetRequired(KnownParameter.ClientSecret, true, out string clientSecret);

					return AzureBlobStorage.FromAzureAd(accountName, tenantId, clientId, clientSecret);
				}

				if (connectionString.Parameters.ContainsKey(KnownParameter.MsiEnabled)) {
					return AzureBlobStorage.FromMsi(accountName);
				}
			}
			else if (connectionString.Prefix == KnownPrefix.AzureDataLakeGen2 || connectionString.Prefix == KnownPrefix.AzureDataLake) {
				connectionString.GetRequired(KnownParameter.AccountName, true, out string accountName);

				string sharedKey = connectionString.Get(KnownParameter.KeyOrPassword);
				if (!string.IsNullOrEmpty(sharedKey)) {
					return AzureDataLakeStorage.FromSharedKey(accountName, sharedKey);
				}

				string tenantId = connectionString.Get(KnownParameter.TenantId);
				if (!string.IsNullOrEmpty(tenantId)) {
					connectionString.GetRequired(KnownParameter.ClientId, true, out string clientId);
					connectionString.GetRequired(KnownParameter.ClientSecret, true, out string clientSecret);

					return AzureDataLakeStorage.FromAzureAd(accountName, tenantId, clientId, clientSecret);
				}

				if (connectionString.Parameters.ContainsKey(KnownParameter.MsiEnabled)) {
					return AzureDataLakeStorage.FromMsi(accountName);
				}

			}

			return null;
		}

		public IQueue CreateMessenger(StorageConnectionString connectionString) => null;
	}
}
