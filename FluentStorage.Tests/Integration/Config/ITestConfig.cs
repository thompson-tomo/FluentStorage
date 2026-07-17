using System;
using System.Net;
using Config.Net;

namespace FluentStorage.Tests.Integration.Config {
	public interface ITestConfig {


		[Option(DefaultValue = "8")]
		string DevOpsVariableSetId { get; }

		string DevOpsPat { get; }

		string ClientId { get; }

		string ClientSecret { get; }

		string TenantId { get; }


		string AzureStorageName { get; }

		string AzureStorageKey { get; }

		string AzureGen2StorageName { get; }

		string AzureGen2StorageKey { get; }

		string OperatorObjectId { get; }

		string AzureServiceBusConnectionString { get; }

		string AzureStorageNativeConnectionString { get; }

		string AzureGen1StorageName { get; }

		Uri AzureKeyVaultUri { get; }



		[Option(Alias = "Aws.AccessKeyId")]
		string AwsAccessKeyId { get; }

		[Option(Alias = "Aws.SecretAccessKey")]
		string AwsSecretAccessKey { get; }

		[Option(Alias = "Aws.TestBucketName")]
		string AwsTestBucketName { get; }

		[Option(Alias = "Aws.TestBucketRegion", DefaultValue = "eu-west-1")]
		string AwsTestBucketRegion { get; }



		[Option(Alias = "Gcp.Storage.BucketName")]
		string GcpStorageBucketName { get; }

		[Option(Alias = "Gcp.Storage.JsonKey")]
		string GcpStorageJsonCreds { get; }




		[Option(Alias = "Mssql.ConnectionString")]
		string MssqlConnectionString { get; }



		[Option(Alias = "Ftp.Hostname")]
		string FtpHostName { get; }

		[Option(Alias = "Ftp.Username")]
		string FtpUsername { get; }

		[Option(Alias = "Ftp.Password")]
		string FtpPassword { get; }

	}

	
}
