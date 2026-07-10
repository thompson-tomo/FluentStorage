using FluentStorage.Storage;
using FluentStorage.Storage.Files;
using FluentStorage.Queue;
using FluentStorage.Queue.Files;

namespace FluentStorage.ConnectionStrings {
	class BuiltInConnectionFactory : IConnectionFactory {
		public IBucket CreateBlobStorage(ConnectionString connectionString) {
			if (connectionString.Prefix == "disk") {
				connectionString.GetRequired("path", true, out string path);

				return new DiskStore(path);
			}

			if (connectionString.Prefix == "inmemory") {
				return new InMemoryBlobStorage();
			}

			if (connectionString.Prefix == "zip") {
				connectionString.GetRequired("path", true, out string path);

				return new ZipStore(path);
			}

			return null;
		}

		public IQueue CreateMessenger(ConnectionString connectionString) {
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
