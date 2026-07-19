namespace FluentStorage.Tests.Integration.Storage {
	public class LocalDiskTestFixture : IStoreFixture {
		protected override IStore CreateStorage(ITestConfig settings) {
			return StorageFactory.Disk(LocalTestDir);
		}
	}

	public class LocalDiskTest : IStoreTest, IClassFixture<LocalDiskTestFixture> {
		public LocalDiskTest(LocalDiskTestFixture fixture) : base(fixture) {
		}
	}
}
