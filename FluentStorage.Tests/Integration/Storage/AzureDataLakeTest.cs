namespace FluentStorage.Tests.Integration.Storage {
	public class AzureDataLakeFixture : IStoreFixture {
		public AzureDataLakeFixture() : base("integration") {

		}

		protected override IStore CreateStorage(ITestConfig settings) {
			return AzureDataLakeStorage.FromSharedKey(
			   settings.AzureDataLakeStorageName,
			   settings.AzureDataLakeStorageKey);
		}
	}

	public class AzureDataLakeTest : IStoreTest, IClassFixture<AzureDataLakeFixture> {
		public AzureDataLakeTest(AzureDataLakeFixture fixture) : base(fixture) {
		}
	}
}
