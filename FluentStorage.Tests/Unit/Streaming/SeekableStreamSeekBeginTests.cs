namespace FluentStorage.Tests.Unit.Streaming;

/// <summary>
/// Unit tests covering Seek() using SeekOrigin.Begin.
/// </summary>
public sealed class SeekableStreamSeekBeginTests {

	/// <summary>
	/// Verifies that seeking to the beginning of the stream positions the
	/// stream at offset zero.
	/// </summary>
	[Fact]
	public void SeekBegin_Zero() {
		using var stream = new SeekableStream(new FakeStore(), "file");

		long position = stream.Seek(0, SeekOrigin.Begin);

		Assert.Equal(0, position);
		Assert.Equal(0, stream.Position);
	}

	/// <summary>
	/// Verifies that seeking to an arbitrary position from the beginning
	/// updates the current position correctly.
	/// </summary>
	[Theory]
	[InlineData(1)]
	[InlineData(100)]
	[InlineData(4096)]
	[InlineData(987654321)]
	public void SeekBegin_Middle(long offset) {
		using var stream = new SeekableStream(new FakeStore(), "file");

		long position = stream.Seek(offset, SeekOrigin.Begin);

		Assert.Equal(offset, position);
		Assert.Equal(offset, stream.Position);
	}

	/// <summary>
	/// Verifies that seeking exactly to the end of a stream with a known
	/// length is allowed.
	/// </summary>
	[Fact]
	public void SeekBegin_EOF() {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file",
			knownLength: 1000);

		long position = stream.Seek(1000, SeekOrigin.Begin);

		Assert.Equal(1000, position);
		Assert.Equal(1000, stream.Position);
	}

	/// <summary>
	/// Verifies that seeking beyond the end of a stream with a known
	/// length is permitted and updates the current position.
	/// </summary>
	[Theory]
	[InlineData(1001)]
	[InlineData(5000)]
	[InlineData(long.MaxValue)]
	public void SeekBegin_BeyondEOF(long offset) {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file",
			knownLength: 1000);

		long position = stream.Seek(offset, SeekOrigin.Begin);

		Assert.Equal(offset, position);
		Assert.Equal(offset, stream.Position);
	}

	/// <summary>
	/// Verifies that attempting to seek before the beginning of the stream
	/// throws an IOException.
	/// </summary>
	[Theory]
	[InlineData(-1)]
	[InlineData(-100)]
	[InlineData(long.MinValue)]
	public void SeekBegin_Negative_Throws(long offset) {
		using var stream = new SeekableStream(new FakeStore(), "file");

		Assert.Throws<IOException>(() =>
			stream.Seek(offset, SeekOrigin.Begin));
	}
}