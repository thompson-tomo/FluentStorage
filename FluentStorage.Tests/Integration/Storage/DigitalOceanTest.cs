using FluentStorage.Tests.Integration.Storage.Fixture;

namespace FluentStorage.Tests.Integration.Storage {
	public class DigitalOceanFixture : StoreFixture {
		protected override IStore CreateStorage(TestConfig settings) {

			if (string.IsNullOrEmpty(TestConfigLoader.Config.DigitalOceanAccessKeyId))
				throw new Exception("Required setting `DigitalOceanAccessKeyId` is blank!");

			if (string.IsNullOrEmpty(TestConfigLoader.Config.DigitalOceanSecretAccessKey))
				throw new Exception("Required setting `DigitalOceanSecretAccessKey` is blank!");

			if (string.IsNullOrEmpty(TestConfigLoader.Config.DigitalOceanBucket))
				throw new Exception("Required setting `DigitalOceanBucketName` is blank!");

			if (string.IsNullOrEmpty(TestConfigLoader.Config.DigitalOceanRegion))
				throw new Exception("Required setting `DigitalOceanRegion` is blank!");

			return DigitalOceanSpacesStorage.FromCredentials(
				settings.DigitalOceanAccessKeyId,
				settings.DigitalOceanSecretAccessKey,
				settings.DigitalOceanBucket,
				settings.DigitalOceanRegion);
		}
	}

	public class DigitalOceanTest : IStoreTest, IClassFixture<DigitalOceanFixture> {
		public DigitalOceanTest(DigitalOceanFixture fixture) : base(fixture) {
		}
	}
}