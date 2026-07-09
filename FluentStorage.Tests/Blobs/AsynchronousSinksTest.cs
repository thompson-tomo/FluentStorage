using FluentStorage.Storage;

namespace FluentStorage.Tests.Blobs.Sink {
	public abstract class AsynchronousSinksTest {
		protected readonly IBucket _storage;

		protected AsynchronousSinksTest(IBucket storage) {
			_storage = storage;
		}
	}
}