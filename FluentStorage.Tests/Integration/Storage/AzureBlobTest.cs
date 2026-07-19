namespace FluentStorage.Tests.Integration.Storage {
	public class AzureBlobFixture : IStoreFixture {
		public AzureBlobFixture() : base("lakeyv12") {
		}

		protected override IStore CreateStorage(ITestConfig settings) {
			return AzureBlobStorage.FromSharedKey(settings.AzureStorageName, settings.AzureStorageKey);
		}
	}

	public class AzureBlobTest : IStoreTest, IClassFixture<AzureBlobFixture> {
		public AzureBlobTest(AzureBlobFixture fixture) : base(fixture) {
		}
	}
}
