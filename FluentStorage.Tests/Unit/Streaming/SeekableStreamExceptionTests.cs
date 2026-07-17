namespace FluentStorage.Tests.Unit.Streaming;

/// <summary>
/// Unit tests covering OpenRange and remote read exception handling.
/// </summary>
public sealed class SeekableStreamExceptionTests {

	/// <summary>
	/// Verifies that exceptions thrown while opening a range are wrapped in an
	/// IOException.
	/// </summary>
	[Fact]
	public void OpenRange_ExceptionWrappedInIOException() {
		var inner = new InvalidOperationException("Boom");

		var store = new FakeStore {
			OpenRangeException = inner
		};

		using var stream = new SeekableStream(store, "file");

		var ex = Assert.Throws<IOException>(() =>
			stream.Read(new byte[1], 0, 1));

		Assert.Same(inner, ex.InnerException);
	}

	/// <summary>
	/// Verifies that the original exception is preserved as the InnerException
	/// when OpenRange fails.
	/// </summary>
	[Fact]
	public void OpenRange_InnerExceptionPreserved() {
		var inner = new Exception("Test");

		var store = new FakeStore {
			OpenRangeException = inner
		};

		using var stream = new SeekableStream(store, "file");

		var ex = Assert.Throws<IOException>(() =>
			stream.Read(new byte[16], 0, 16));

		Assert.Same(inner, ex.InnerException);
	}

	/// <summary>
	/// Verifies that exceptions thrown while reading the remote stream are
	/// wrapped in an IOException.
	/// </summary>
	[Fact]
	public void RemoteRead_ExceptionWrappedInIOException() {
		var inner = new InvalidOperationException("Boom");

		var store = new FakeStore(new byte[100]) {
			RemoteReadException = inner
		};

		using var stream = new SeekableStream(store, "file");

		var ex = Assert.Throws<IOException>(() =>
			stream.Read(new byte[100], 0, 100));

		Assert.Same(inner, ex.InnerException);
	}

	/// <summary>
	/// Verifies that the original exception is preserved as the InnerException
	/// when reading from the remote stream fails.
	/// </summary>
	[Fact]
	public void RemoteRead_InnerExceptionPreserved() {
		var inner = new Exception("Failure");

		var store = new FakeStore(new byte[100]) {
			RemoteReadException = inner
		};

		using var stream = new SeekableStream(store, "file");

		var ex = Assert.Throws<IOException>(() =>
			stream.Read(new byte[100], 0, 100));

		Assert.Same(inner, ex.InnerException);
	}

	/// <summary>
	/// Verifies that OperationCanceledException thrown while opening a range is
	/// propagated without being wrapped.
	/// </summary>
	[Fact]
	public void OpenRange_OperationCanceled_NotWrapped() {
		var store = new FakeStore {
			OpenRangeException = new OperationCanceledException()
		};

		using var stream = new SeekableStream(store, "file");

		Assert.Throws<OperationCanceledException>(() =>
			stream.Read(new byte[1], 0, 1));
	}

	/// <summary>
	/// Verifies that OperationCanceledException thrown while reading the remote
	/// stream is propagated without being wrapped.
	/// </summary>
	[Fact]
	public void RemoteRead_OperationCanceled_NotWrapped() {
		var store = new FakeStore(new byte[100]) {
			RemoteReadException = new OperationCanceledException()
		};

		using var stream = new SeekableStream(store, "file");

		Assert.Throws<OperationCanceledException>(() =>
			stream.Read(new byte[100], 0, 100));
	}
}