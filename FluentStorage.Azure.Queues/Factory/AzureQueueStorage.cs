using System;
using FluentStorage.ConnectionString;
using FluentStorage.Queue;
using FluentStorage.Azure.Queues;

namespace FluentStorage {
	public static class AzureQueueStorage {
		/// <summary>
		/// Register Azure module.
		/// </summary>
		public static void Use() {
			StorageFactory.Use(new Module());
		}

		/// <summary>
		/// Creates an instance of a publisher to Azure Storage Queues
		/// </summary>
		/// <param name="factory">Factory reference</param>
		/// <param name="accountName">Account name. Must not be <see langword="null"/> or empty.</param>
		/// <param name="storageKey">Storage key. Must not be <see langword="null"/> or empty.</param>
		/// <param name="serviceUri">Alternative service uri. Pass <see langword="null"/> for default.</param>
		/// <returns>Generic message publisher interface</returns>
		public static IQueue FromCredentials(
		   string accountName,
		   string storageKey,
		   Uri serviceUri = null) {
			if (serviceUri == null)
				return new AzureStorageQueueMessenger(accountName, storageKey);

			return new AzureStorageQueueMessenger(accountName, storageKey, serviceUri);
		}

		/// <summary>
		/// Create a new connection string to connect to Azure Queue
		/// </summary>
		public static StorageConnectionString CreateConnectionStringFromSharedKey(
		   string accountName,
		   string accountKey) {
			var cs = new StorageConnectionString(KnownPrefix.AzureQueueStorage);
			cs.Parameters[KnownParameter.AccountName] = accountName;
			cs.Parameters[KnownParameter.KeyOrPassword] = accountKey;
			return cs;
		}
	}
}
