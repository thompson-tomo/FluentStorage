namespace FluentStorage.Tests.Unit.Streaming;

/// <summary>
/// Unit tests covering the Length property of SeekableStream.
/// </summary>
public sealed class SeekableStreamLengthTests {

	/// <summary>
	/// Verifies that the Length property returns the value supplied to the
	/// constructor when the object length is already known.
	/// </summary>
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(123)]
	[InlineData(4096)]
	[InlineData(987654321)]
	public void Length_Known_ReturnsValue(long length) {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file",
			knownLength: length);

		Assert.Equal(length, stream.Length);
	}

	/// <summary>
	/// Verifies that accessing the Length property before the end of the object
	/// has been discovered throws a NotSupportedException.
	/// </summary>
	[Fact]
	public void Length_Unknown_Throws() {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file");

		Assert.Throws<NotSupportedException>(() => _ = stream.Length);
	}

	/// <summary>
	/// Verifies that reading a final partial buffer discovers the object length
	/// and makes the Length property available.
	/// </summary>
	[Fact]
	public void Length_DiscoveredAfterShortRead() {
		byte[] data = new byte[100];

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 256);

		byte[] buffer = new byte[512];

		int read = stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(100, read);
		Assert.Equal(100, stream.Length);
	}

	/// <summary>
	/// Verifies that attempting to read at the end of an empty object discovers
	/// the object length as zero.
	/// </summary>
	[Fact]
	public void Length_DiscoveredAfterEmptyReadAtEOF() {
		using var stream = new SeekableStream(
			new FakeStore(Array.Empty<byte>()),
			"file");

		byte[] buffer = new byte[1];

		int read = stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(0, read);
		Assert.Equal(0, stream.Length);
	}

	/// <summary>
	/// Verifies that accessing the Length property after the stream has been
	/// disposed throws an ObjectDisposedException.
	/// </summary>
	[Fact]
	public void Length_AfterDispose_Throws() {
		var stream = new SeekableStream(
			new FakeStore(),
			"file",
			knownLength: 100);

		stream.Dispose();

		Assert.Throws<ObjectDisposedException>(() => _ = stream.Length);
	}
}