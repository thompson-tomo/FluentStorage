using YamlDotNet.Serialization;

namespace FluentStorage.Tests.Integration.Config {
	[YamlSerializable]
	public class TestConfig {

		// ----------------------------------------------------------------------
		// Local Disk
		// ----------------------------------------------------------------------

		public string LocalDiskPath { get; set; }


		// ---------------------------------------------------------------------
		// Azure Blob/Files/DataLake
		// ---------------------------------------------------------------------


		public string AzureClientId { get; set; }

		public string AzureClientSecret { get; set; }

		public string AzureTenantId { get; set; }
		public string AzureStorageName { get; set; }

		public string AzureStorageKey { get; set; }

		public string AzureDataLakeStorageName { get; set; }

		public string AzureDataLakeStorageKey { get; set; }

		public string AzureDataLakeOperatorObjectId { get; set; }

		public string AzureServiceBusConnectionString { get; set; }

		public Uri AzureKeyVaultUri { get; set; }



		// ---------------------------------------------------------------------
		// AWS S3
		// ---------------------------------------------------------------------

		public string AwsAccessKeyId { get; set; }

		public string AwsSecretAccessKey { get; set; }

		public string AwsBucketName { get; set; }

		public string AwsBucketRegion { get; set; }



		// ---------------------------------------------------------------------
		// GCP Storage
		// ---------------------------------------------------------------------

		public string GcpBucketName { get; set; }

		public string GcpJsonKey { get; set; }



		// ---------------------------------------------------------------------
		// FTP
		// ---------------------------------------------------------------------

		public string FtpHost { get; set; }

		public string FtpUsername { get; set; }

		public string FtpPassword { get; set; }



		// ---------------------------------------------------------------------
		// SFTP
		// ---------------------------------------------------------------------

		public string SftpHost { get; set; }
		public int SftpPort { get; set; }
		public string SftpUser { get; set; }
		public string SftpPassphrase { get; set; }
		public string SftpPrivateKeyPath { get; set; }



		public TestConfig() {
		}
	}

	
}
