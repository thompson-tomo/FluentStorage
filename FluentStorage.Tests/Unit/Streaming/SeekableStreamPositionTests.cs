namespace FluentStorage.Tests.Unit.Streaming;

/// <summary>
/// Unit tests covering the Position property of SeekableStream.
/// </summary>
public sealed class SeekableStreamPositionTests {

	/// <summary>
	/// Verifies that a newly constructed stream starts at position zero.
	/// </summary>
	[Fact]
	public void Position_InitiallyZero() {
		using var stream = new SeekableStream(new FakeStore(), "file");

		Assert.Equal(0, stream.Position);
	}

	/// <summary>
	/// Verifies that assigning to the Position property updates the current
	/// stream position.
	/// </summary>
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(123)]
	[InlineData(4096)]
	[InlineData(987654321)]
	public void Position_SetBegin_UpdatesPosition(long position) {
		using var stream = new SeekableStream(new FakeStore(), "file");

		stream.Position = position;

		Assert.Equal(position, stream.Position);
	}

	/// <summary>
	/// Verifies that seeking relative to the current position updates the
	/// Position property correctly.
	/// </summary>
	[Fact]
	public void Position_AfterSeekCurrent_UpdatesPosition() {
		using var stream = new SeekableStream(new FakeStore(), "file");

		stream.Seek(100, SeekOrigin.Begin);
		stream.Seek(50, SeekOrigin.Current);

		Assert.Equal(150, stream.Position);
	}

	/// <summary>
	/// Verifies that seeking relative to the end of a stream with a known
	/// length updates the Position property correctly.
	/// </summary>
	[Fact]
	public void Position_AfterSeekEnd_UpdatesPosition() {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file",
			knownLength: 1000);

		stream.Seek(-25, SeekOrigin.End);

		Assert.Equal(975, stream.Position);
	}

	/// <summary>
	/// Verifies that multiple consecutive seek operations produce the expected
	/// final position.
	/// </summary>
	[Fact]
	public void Position_AfterMultipleSeeks_Correct() {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file",
			knownLength: 1000);

		stream.Seek(100, SeekOrigin.Begin);
		stream.Seek(50, SeekOrigin.Current);
		stream.Seek(-10, SeekOrigin.Current);
		stream.Seek(-100, SeekOrigin.End);

		Assert.Equal(900, stream.Position);
	}

	/// <summary>
	/// Verifies that assigning a negative value to the Position property throws
	/// an IOException.
	/// </summary>
	[Theory]
	[InlineData(-1)]
	[InlineData(-100)]
	[InlineData(long.MinValue)]
	public void Position_SetNegative_Throws(long position) {
		using var stream = new SeekableStream(new FakeStore(), "file");

		Assert.Throws<IOException>(() => stream.Position = position);
	}

	/// <summary>
	/// Verifies that assigning a value beyond the end of a stream with a known
	/// length is allowed and updates the Position property.
	/// </summary>
	[Theory]
	[InlineData(1001)]
	[InlineData(5000)]
	[InlineData(long.MaxValue)]
	public void Position_SetBeyondEOF_UpdatesPosition(long position) {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file",
			knownLength: 1000);

		stream.Position = position;

		Assert.Equal(position, stream.Position);
	}

	/// <summary>
	/// Verifies that repeatedly assigning to the Position property always
	/// reflects the most recent value.
	/// </summary>
	[Fact]
	public void Position_MultipleAssignments_Correct() {
		using var stream = new SeekableStream(new FakeStore(), "file");

		stream.Position = 10;
		Assert.Equal(10, stream.Position);

		stream.Position = 500;
		Assert.Equal(500, stream.Position);

		stream.Position = 0;
		Assert.Equal(0, stream.Position);

		stream.Position = 42;
		Assert.Equal(42, stream.Position);
	}
}