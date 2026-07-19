namespace FluentStorage.Tests.Integration.Storage {
	public class GcpFixture : IStoreFixture {
		protected override IStore CreateStorage(ITestConfig settings) {
			return GoogleCloudStorage.FromJson(
			   settings.GcpBucketName,
			   settings.GcpJsonKey,
			   true);
		}
	}

	public class GcpTest : IStoreTest, IClassFixture<GcpFixture> {
		public GcpTest(GcpFixture fixture) : base(fixture) {

		}
	}
}
