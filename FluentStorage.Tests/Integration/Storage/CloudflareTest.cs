using FluentStorage.Tests.Integration.Storage.Fixture;

namespace FluentStorage.Tests.Integration.Storage {
	public class CloudflareFixture : StoreFixture {
		protected override IStore CreateStorage(TestConfig settings) {

			if (string.IsNullOrEmpty(TestConfigLoader.Config.CloudflareAccessKeyId))
				throw new Exception("Required setting `CloudflareAccessKeyId` is blank!");

			if (string.IsNullOrEmpty(TestConfigLoader.Config.CloudflareSecretAccessKey))
				throw new Exception("Required setting `CloudflareSecretAccessKey` is blank!");

			if (string.IsNullOrEmpty(TestConfigLoader.Config.CloudflareBucket))
				throw new Exception("Required setting `CloudflareBucketName` is blank!");

			if (string.IsNullOrEmpty(TestConfigLoader.Config.CloudflareAccountId))
				throw new Exception("Required setting `CloudflareAccountId` is blank!");

			return CloudflareR2Storage.FromCredentials(
				settings.CloudflareAccessKeyId,
				settings.CloudflareSecretAccessKey,
				settings.CloudflareBucket,
				settings.CloudflareAccountId);
		}
	}

	public class CloudflareTest : IStoreTest, IClassFixture<CloudflareFixture> {
		public CloudflareTest(CloudflareFixture fixture) : base(fixture) {
		}
	}
}