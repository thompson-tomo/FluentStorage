using FluentStorage.Storage;

namespace FluentStorage.Tests.Blobs.Sink {
	public abstract class AsynchronousSinksTest {
		protected readonly IStore _storage;

		protected AsynchronousSinksTest(IStore storage) {
			_storage = storage;
		}
	}
}