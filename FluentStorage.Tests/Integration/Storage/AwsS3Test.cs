namespace FluentStorage.Tests.Integration.Storage {
	public class AwsS3Fixture : IStoreFixture {
		protected override IStore CreateStorage(ITestConfig settings) {
			return AwsS3Storage.FromCredentials(
					 settings.AwsAccessKeyId,
					 settings.AwsSecretAccessKey,
					 null,
					 settings.AwsBucketName,
					 settings.AwsBucketRegion);
		}
	}

	public class AwsS3Test : IStoreTest, IClassFixture<AwsS3Fixture> {
		public AwsS3Test(AwsS3Fixture fixture) : base(fixture) {
		}
	}
}
