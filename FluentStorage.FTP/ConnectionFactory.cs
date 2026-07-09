using System.Net;
using FluentStorage.Storage;
using FluentStorage.ConnectionString;
using FluentStorage.Queue;

namespace FluentStorage.FTP {
	class ConnectionFactory : IConnectionFactory {
		public IBucket CreateBlobStorage(StorageConnectionString connectionString) {
			if (connectionString.Prefix == "ftp") {
				connectionString.GetRequired("host", true, out string host);
				connectionString.GetRequired("user", true, out string user);
				connectionString.GetRequired("password", true, out string password);

				return new FluentFtpBlobStorage(host, new NetworkCredential(user, password));
			}

			return null;
		}

		public IQueue CreateMessenger(StorageConnectionString connectionString) => null;
	}
}
