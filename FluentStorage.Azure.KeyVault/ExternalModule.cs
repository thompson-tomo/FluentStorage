using System;
using FluentStorage.Storage;
using FluentStorage.ConnectionString;
using FluentStorage.Queue;

namespace FluentStorage.Azure.KeyVault {
	class ExternalModule : IExternalModule, IConnectionFactory {
		public IConnectionFactory ConnectionFactory => this;

		public IBucket CreateBlobStorage(StorageConnectionString connectionString) {
			if (connectionString.Prefix == KnownPrefix.AzureKeyVault) {
				connectionString.GetRequired(KnownParameter.VaultUri, true, out string uri);

				if (connectionString.Parameters.ContainsKey(KnownParameter.MsiEnabled)) {
					return AzureKeyVaultStorage.FromMsi(new Uri(uri));
				}
				else {
					connectionString.GetRequired(KnownParameter.TenantId, true, out string tenantId);
					connectionString.GetRequired(KnownParameter.ClientId, true, out string clientId);
					connectionString.GetRequired(KnownParameter.ClientSecret, true, out string clientSecret);

					return AzureKeyVaultStorage.FromCredentials(new Uri(uri), tenantId, clientId, clientSecret);
				}
			}

			return null;
		}

		public IQueue CreateMessenger(StorageConnectionString connectionString) => null;
	}
}
