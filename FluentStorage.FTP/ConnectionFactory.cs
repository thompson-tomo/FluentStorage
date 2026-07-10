using System.Net;
using FluentStorage.Storage;
using FluentStorage.ConnectionStrings;
using FluentStorage.Queue;
using FluentStorage.FTP.Storage;

namespace FluentStorage.FTP {
	class ConnectionFactory : IConnectionFactory {
		public IBucket CreateBlobStorage(ConnectionString connectionString) {
			if (connectionString.Prefix == "ftp") {
				connectionString.GetRequired("host", true, out string host);
				connectionString.GetRequired("user", true, out string user);
				connectionString.GetRequired("password", true, out string password);

				return new FtpStore(host, new NetworkCredential(user, password));
			}

			return null;
		}

		public IQueue CreateMessenger(ConnectionString connectionString) => null;
	}
}
