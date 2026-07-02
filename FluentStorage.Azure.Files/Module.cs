using FluentStorage.Blobs;
using FluentStorage.ConnectionString;
using FluentStorage.Messaging;

namespace FluentStorage.Azure.Files {
	class Module : IExternalModule, IConnectionFactory {
		public IConnectionFactory ConnectionFactory => this;

		public IBlobStorage CreateBlobStorage(StorageConnectionString connectionString) {
			if (connectionString.Prefix == KnownPrefix.AzureFilesStorage) {
				connectionString.GetRequired(KnownParameter.AccountName, true, out string accountName);

				string sharedKey = connectionString.Get(KnownParameter.KeyOrPassword);
				if (!string.IsNullOrEmpty(sharedKey)) {
					return StorageFactory.Blobs.AzureFilesWithSharedKey(accountName, sharedKey);
				}

				string tenantId = connectionString.Get(KnownParameter.TenantId);
				if (!string.IsNullOrEmpty(tenantId)) {
					connectionString.GetRequired(KnownParameter.ClientId, true, out string clientId);
					connectionString.GetRequired(KnownParameter.ClientSecret, true, out string clientSecret);

					return StorageFactory.Blobs.AzureFilesWithAzureAd(accountName, tenantId, clientId, clientSecret);
				}

				if (connectionString.Parameters.ContainsKey(KnownParameter.MsiEnabled)) {
					return StorageFactory.Blobs.AzureFilesWithMsi(accountName);
				}
			}

			return null;
		}

		public IMessenger CreateMessenger(StorageConnectionString connectionString) => null;
	}
}
