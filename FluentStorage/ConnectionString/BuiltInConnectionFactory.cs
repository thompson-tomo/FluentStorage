using FluentStorage.Storage;
using FluentStorage.Storage.Files;
using FluentStorage.Queue;
using FluentStorage.Queue.Files;

namespace FluentStorage.ConnectionString {
	class BuiltInConnectionFactory : IConnectionFactory {
		public IBucket CreateBlobStorage(StorageConnectionString connectionString) {
			if (connectionString.Prefix == "disk") {
				connectionString.GetRequired("path", true, out string path);

				return new DiskDirectoryBlobStorage(path);
			}

			if (connectionString.Prefix == "inmemory") {
				return new InMemoryBlobStorage();
			}

			if (connectionString.Prefix == "zip") {
				connectionString.GetRequired("path", true, out string path);

				return new ZipFileBlobStorage(path);
			}

			return null;
		}

		public IQueue CreateMessenger(StorageConnectionString connectionString) {
			if (connectionString.Prefix == "inmemory") {
				connectionString.GetRequired("name", true, out string name);

				return InMemoryMessenger.CreateOrGet(name);
			}

			if (connectionString.Prefix == "disk") {
				connectionString.GetRequired("path", true, out string path);

				return new LocalDiskMessenger(path);
			}

			return null;
		}
	}
}
