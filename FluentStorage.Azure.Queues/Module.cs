using FluentStorage.Storage;
using FluentStorage.ConnectionString;
using FluentStorage.Queue;

namespace FluentStorage.Azure.Queues {
	class Module : IExternalModule, IConnectionFactory {
		public IConnectionFactory ConnectionFactory => this;

		public IBucket CreateBlobStorage(StorageConnectionString connectionString) => null;

		public IQueue CreateMessenger(StorageConnectionString connectionString) {
			if (connectionString.Prefix == KnownPrefix.AzureQueueStorage) {
				connectionString.GetRequired(KnownParameter.AccountName, true, out string accountName);
				connectionString.GetRequired(KnownParameter.KeyOrPassword, true, out string key);

				return new AzureStorageQueueMessenger(accountName, key);
			}

			return null;
		}

	}
}
