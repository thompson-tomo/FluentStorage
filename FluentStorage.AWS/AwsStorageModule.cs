using System;
using FluentStorage.AWS.Blobs;
using FluentStorage.Storage;
using FluentStorage.ConnectionString;
using FluentStorage.Queue;

namespace FluentStorage.AWS {
	class AwsStorageModule : IExternalModule, IConnectionFactory {
		public IConnectionFactory ConnectionFactory => this;

		public IBucket CreateBlobStorage(StorageConnectionString connectionString) {

			// handle service specific prefixes
			if (KnownPrefix.IsS3Compatible(connectionString.Prefix)) {

				string region = String.Empty;

				string cliProfileName = connectionString.Get(KnownParameter.LocalProfileName);
				connectionString.GetRequired(KnownParameter.BucketName, true, out string bucket);

				if (string.IsNullOrEmpty(cliProfileName)) {
					string keyId = connectionString.Get(KnownParameter.KeyId);
					string key = connectionString.Get(KnownParameter.KeyOrPassword);

					if (string.IsNullOrEmpty(keyId) != string.IsNullOrEmpty(key)) {
						throw new ArgumentException($"connection string requires both 'key' and 'keyId' parameters, or neither.");
					}

					if (string.IsNullOrEmpty(keyId)) {
						connectionString.GetRequired(KnownParameter.Region, true, out region);

						return new S3Store(bucket, region);
					}

					// get region and/or serviceUrl options from connection string ...

					var serviceUrl = connectionString.Get(KnownParameter.ServiceUrl);
					region = connectionString.Get(KnownParameter.Region);

					// only one or the other is allowed simultaneously, so throw if both are specified

					if (!String.IsNullOrWhiteSpace(serviceUrl) && !String.IsNullOrWhiteSpace(region)) {
						throw new ArgumentException($"connection string can have either 'region' or 'serviceUrl' parameters, but not both.");

					}

					string sessionToken = connectionString.Get(KnownParameter.SessionToken);



					// [ADD STORAGE PROVIDER]

					// USE SPECIAL CONSTRUCTORS for special providers

					if (connectionString.Prefix == KnownPrefix.MinIoS3) {
						return S3Store.FromMinIO(keyId, key, bucket, region, serviceUrl, sessionToken);
					}
					else if (connectionString.Prefix == KnownPrefix.CloudflareR2) {
						string accountId = connectionString.Get(KnownParameter.AccountId);
						return S3Store.FromCloudflareR2(keyId, key, bucket, accountId);
					}
					else if (connectionString.Prefix == KnownPrefix.Wasabi) {
						return S3Store.FromWasabi(keyId, key, bucket, serviceUrl, sessionToken);
					}
					else if (connectionString.Prefix == KnownPrefix.DigitalOceanSpaces) {
						return S3Store.FromDigitalOcean(keyId, key, bucket, region, sessionToken);
					}
					else if (connectionString.Prefix == KnownPrefix.BackblazeB2) {
						return S3Store.FromBackblazeB2(keyId, key, bucket, region);
					}
					else if (connectionString.Prefix == KnownPrefix.Hetzner) {
						return S3Store.FromHetzner(keyId, key, bucket, region);
					}
					else if (connectionString.Prefix == KnownPrefix.Vultr) {
						string hostName = connectionString.Get(KnownParameter.HostName);
						return S3Store.FromVultr(keyId, key, bucket, hostName);
					}

					// fallback to S3 constructor if its not a special providr

					else if (connectionString.Prefix == KnownPrefix.AwsS3) {
						return new S3Store(keyId, key, sessionToken, bucket, region, serviceUrl);
					}

				}
#if !NET16
				else {
					return S3Store.FromAwsCliProfile(cliProfileName, bucket, region);
				}
#endif
			}


			return null;
		}

		public IQueue CreateMessenger(StorageConnectionString connectionString) => null;
	}
}