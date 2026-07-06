using System;
using System.Net;
using FluentFTP;
using FluentStorage.Blobs;
using FluentStorage.ConnectionString;
using FluentStorage.FTP;

namespace FluentStorage {
	/// <summary>
	/// FluentFTP factory that is accessible using `FluentStorage.StorageFactory.Blobs` by way of extension methods.
	/// </summary>
	public static class Factory {
		/// <summary>
		/// Register Azure module.
		/// </summary>
		public static IModulesFactory UseFtpStorage(this IModulesFactory factory) {
			return factory.Use(new Module());
		}

		private class Module : IExternalModule {
			public IConnectionFactory ConnectionFactory => new ConnectionFactory();
		}

		/// <summary>
		/// Constructs an instance of FTP client from host name and credentials
		/// </summary>
		public static IBlobStorage Ftp(this IBlobStorageFactory factory,
		   string hostNameOrAddress, NetworkCredential credentials,
		   FtpDataConnectionType dataConnectionType = FtpDataConnectionType.AutoActive) {
			return new FluentFtpBlobStorage(hostNameOrAddress, credentials, dataConnectionType);
		}

		/// <summary>
		/// Constructs an instance of FTP client by accepting a custom instance of FluentFTP client
		/// </summary>
		public static IBlobStorage FtpFromFluentFtpClient(this IBlobStorageFactory factory,
		   AsyncFtpClient ftpClient) {
			return new FluentFtpBlobStorage(ftpClient, false);
		}
	}
}
