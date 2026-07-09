using System;
using System.Net;
using FluentFTP;
using FluentStorage.Storage;
using FluentStorage.ConnectionString;
using FluentStorage.FTP;

namespace FluentStorage {
	/// <summary>
	/// FluentFTP factory that is accessible using `FluentStorage.StorageFactory.Blobs` by way of extension methods.
	/// </summary>
	public static class FtpStorage {
		/// <summary>
		/// Register Azure module.
		/// </summary>
		public static void Use() {
			FluentStorage.StorageFactory.Use(new Module());
		}

		private class Module : IExternalModule {
			public IConnectionFactory ConnectionFactory => new ConnectionFactory();
		}

		/// <summary>
		/// Constructs an instance of FTP client from host name and credentials
		/// </summary>
		public static IBucket FromCredentials(
		   string hostNameOrAddress, NetworkCredential credentials,
		   FtpDataConnectionType dataConnectionType = FtpDataConnectionType.AutoActive) {
			return new FluentFtpBlobStorage(hostNameOrAddress, credentials, dataConnectionType);
		}

		/// <summary>
		/// Constructs an instance of FTP client by accepting a custom instance of FluentFTP client
		/// </summary>
		public static IBucket FromClient(
		   AsyncFtpClient ftpClient) {
			return new FluentFtpBlobStorage(ftpClient, false);
		}

	}
}
