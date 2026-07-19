namespace FluentStorage.Tests.Integration.Storage {
	public class MemoryFixture : IStoreFixture {
		protected override IStore CreateStorage(ITestConfig settings) {
			return StorageFactory.InMemory();
		}
	}

	public class MemoryTest : IStoreTest, IClassFixture<MemoryFixture> {
		public MemoryTest(MemoryFixture fixture) : base(fixture) {
		}
	}
}
