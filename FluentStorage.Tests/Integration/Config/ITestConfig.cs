namespace FluentStorage.Tests.Integration.Config {
	public interface ITestConfig {

		// ---------------------------------------------------------------------
		// Azure Blob/Files/DataLake
		// ---------------------------------------------------------------------


		string AzureClientId { get; }

		string AzureClientSecret { get; }

		string AzureTenantId { get; }
		string AzureStorageName { get; }

		string AzureStorageKey { get; }

		string AzureDataLakeStorageName { get; }

		string AzureDataLakeStorageKey { get; }

		string AzureDataLakeOperatorObjectId { get; }

		string AzureServiceBusConnectionString { get; }

		Uri AzureKeyVaultUri { get; }



		// ---------------------------------------------------------------------
		// AWS S3
		// ---------------------------------------------------------------------

		string AwsAccessKeyId { get; }

		string AwsSecretAccessKey { get; }

		string AwsBucketName { get; }

		string AwsBucketRegion { get; }



		// ---------------------------------------------------------------------
		// GCP Storage
		// ---------------------------------------------------------------------

		string GcpBucketName { get; }

		string GcpJsonKey { get; }



		// ---------------------------------------------------------------------
		// FTP
		// ---------------------------------------------------------------------

		string FtpHost { get; }

		string FtpUsername { get; }

		string FtpPassword { get; }



		// ---------------------------------------------------------------------
		// SFTP
		// ---------------------------------------------------------------------

		string SftpHost { get; }
		int SftpPort { get; }
		string SftpUser { get; }
		string SftpPassphrase { get; }

	}

	
}
