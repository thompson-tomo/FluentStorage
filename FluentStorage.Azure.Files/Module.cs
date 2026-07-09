using FluentStorage.Storage;
using FluentStorage.ConnectionString;
using FluentStorage.Queue;

namespace FluentStorage.Azure.Files {
	class Module : IExternalModule, IConnectionFactory {
		public IConnectionFactory ConnectionFactory => this;

		public IBucket CreateBlobStorage(StorageConnectionString connectionString) {
			if (connectionString.Prefix == KnownPrefix.AzureFilesStorage) {
				connectionString.GetRequired(KnownParameter.AccountName, true, out string accountName);

				string sharedKey = connectionString.Get(KnownParameter.KeyOrPassword);
				if (!string.IsNullOrEmpty(sharedKey)) {
					return AzureFilesStorage.FromSharedKey(accountName, sharedKey);
				}

				string tenantId = connectionString.Get(KnownParameter.TenantId);
				if (!string.IsNullOrEmpty(tenantId)) {
					connectionString.GetRequired(KnownParameter.ClientId, true, out string clientId);
					connectionString.GetRequired(KnownParameter.ClientSecret, true, out string clientSecret);

					return AzureFilesStorage.FromAzureAd(accountName, tenantId, clientId, clientSecret);
				}

				if (connectionString.Parameters.ContainsKey(KnownParameter.MsiEnabled)) {
					return AzureFilesStorage.FromMsi(accountName);
				}
			}

			return null;
		}

		public IQueue CreateMessenger(StorageConnectionString connectionString) => null;
	}
}
