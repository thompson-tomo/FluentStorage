namespace FluentStorage.Tests.Integration.Storage {
	public class AzureFilesFixture : IStoreFixture {
		public AzureFilesFixture() : base("testshare") {

		}

		protected override IStore CreateStorage(ITestConfig settings) {
			return AzureFilesStorage.FromCredentials(settings.AzureStorageName, settings.AzureStorageKey);
		}
	}

	public class AzureFilesTest : IStoreTest, IClassFixture<AzureFilesFixture> {
		public AzureFilesTest(AzureFilesFixture fixture) : base(fixture) {

		}
	}
}
