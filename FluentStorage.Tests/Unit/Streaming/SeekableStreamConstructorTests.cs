namespace FluentStorage.Tests.Unit.Streaming;

/// <summary>
/// Unit tests covering construction of SeekableStream.
/// </summary>
public sealed class SeekableStreamConstructorTests {

	/// <summary>
	/// Verifies that passing a null store throws an ArgumentNullException.
	/// </summary>
	[Fact]
	public void Ctor_NullStore_Throws() {
		Assert.Throws<ArgumentNullException>(() =>
			new SeekableStream(null!, "file"));
	}

	/// <summary>
	/// Verifies that passing a null object path throws an ArgumentNullException.
	/// </summary>
	[Fact]
	public void Ctor_NullPath_Throws() {
		var store = new FakeStore();

		Assert.Throws<ArgumentNullException>(() =>
			new SeekableStream(store, null!));
	}

	/// <summary>
	/// Verifies that a buffer size of zero is rejected.
	/// </summary>
	[Fact]
	public void Ctor_BufferSizeZero_Throws() {
		var store = new FakeStore();

		Assert.Throws<ArgumentOutOfRangeException>(() =>
			new SeekableStream(store, "file", bufferSize: 0));
	}

	/// <summary>
	/// Verifies that negative buffer sizes are rejected.
	/// </summary>
	[Theory]
	[InlineData(-1)]
	[InlineData(-64)]
	[InlineData(-4096)]
	[InlineData(int.MinValue)]
	public void Ctor_BufferSizeNegative_Throws(int bufferSize) {
		var store = new FakeStore();

		Assert.Throws<ArgumentOutOfRangeException>(() =>
			new SeekableStream(store, "file", bufferSize));
	}

	/// <summary>
	/// Verifies that constructing the stream with default arguments succeeds
	/// and initializes the stream to its default state.
	/// </summary>
	[Fact]
	public void Ctor_DefaultValues_Initializes() {
		var store = new FakeStore();

		using var stream = new SeekableStream(store, "file");

		Assert.Equal(0, stream.Position);
		Assert.True(stream.CanRead);
		Assert.True(stream.CanSeek);
		Assert.False(stream.CanWrite);
		Assert.False(stream.CanTimeout);
		Assert.Throws<NotSupportedException>(() => _ = stream.Length);
	}

	/// <summary>
	/// Verifies that valid custom buffer sizes are accepted.
	/// </summary>
	[Theory]
	[InlineData(1)]
	[InlineData(512)]
	[InlineData(4096)]
	[InlineData(65536)]
	[InlineData(1024 * 1024)]
	public void Ctor_CustomBufferSize_Initializes(int bufferSize) {
		var store = new FakeStore();

		using var stream = new SeekableStream(store, "file", bufferSize);

		Assert.Equal(0, stream.Position);
		Assert.True(stream.CanRead);
		Assert.True(stream.CanSeek);
		Assert.False(stream.CanWrite);
		Assert.False(stream.CanTimeout);
	}

	/// <summary>
	/// Verifies that providing a known object length makes the Length
	/// property immediately available.
	/// </summary>
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(123)]
	[InlineData(4096)]
	[InlineData(987654321)]
	public void Ctor_KnownLength_Initializes(long knownLength) {
		var store = new FakeStore();

		using var stream = new SeekableStream(
			store,
			"file",
			knownLength: knownLength);

		Assert.Equal(knownLength, stream.Length);
		Assert.Equal(0, stream.Position);
	}

	/// <summary>
	/// Verifies that omitting the object length leaves the Length property
	/// unavailable until the end of the stream has been discovered.
	/// </summary>
	[Fact]
	public void Ctor_UnknownLength_Initializes() {
		var store = new FakeStore();

		using var stream = new SeekableStream(store, "file");

		Assert.Equal(0, stream.Position);
		Assert.Throws<NotSupportedException>(() => _ = stream.Length);
	}
}