namespace FluentStorage.Tests.Unit.Streaming;

/// <summary>
/// Unit tests covering Seek() using SeekOrigin.End and general seek behaviour.
/// </summary>
public sealed class SeekableStreamSeekEndTests {

	/// <summary>
	/// Verifies that seeking to the end of a stream with a known length positions
	/// the stream at the end of the object.
	/// </summary>
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(100)]
	[InlineData(4096)]
	public void SeekEnd_KnownLength_Zero(long length) {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file",
			knownLength: length);

		long position = stream.Seek(0, SeekOrigin.End);

		Assert.Equal(length, position);
		Assert.Equal(length, stream.Position);
	}

	/// <summary>
	/// Verifies that seeking backwards from the end of a stream with a known
	/// length updates the position correctly.
	/// </summary>
	[Theory]
	[InlineData(1000, -1, 999)]
	[InlineData(1000, -100, 900)]
	[InlineData(4096, -4096, 0)]
	[InlineData(1, -1, 0)]
	public void SeekEnd_KnownLength_Negative(long length, long offset, long expectedPosition) {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file",
			knownLength: length);

		long position = stream.Seek(offset, SeekOrigin.End);

		Assert.Equal(expectedPosition, position);
		Assert.Equal(expectedPosition, stream.Position);
	}

	/// <summary>
	/// Verifies that seeking beyond the end of a stream with a known length is
	/// permitted.
	/// </summary>
	[Theory]
	[InlineData(1000, 1)]
	[InlineData(1000, 500)]
	[InlineData(4096, 1024)]
	public void SeekEnd_KnownLength_BeyondEOF(long length, long offset) {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file",
			knownLength: length);

		long expected = length + offset;

		long position = stream.Seek(offset, SeekOrigin.End);

		Assert.Equal(expected, position);
		Assert.Equal(expected, stream.Position);
	}

	/// <summary>
	/// Verifies that seeking relative to the end of a stream whose length is
	/// unknown throws a NotSupportedException.
	/// </summary>
	[Theory]
	[InlineData(-1)]
	[InlineData(0)]
	[InlineData(1)]
	public void SeekEnd_UnknownLength_Throws(long offset) {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file");

		Assert.Throws<NotSupportedException>(() =>
			stream.Seek(offset, SeekOrigin.End));
	}

	/// <summary>
	/// Verifies that seeking never triggers a call to OpenRange. Seeking should
	/// only update the logical stream position.
	/// </summary>
	[Fact]
	public void Seek_DoesNotOpenRange() {
		var store = new FakeStore();

		using var stream = new SeekableStream(store, "file");

		stream.Seek(100, SeekOrigin.Begin);
		stream.Seek(50, SeekOrigin.Current);
		stream.Seek(1000, SeekOrigin.Begin);

		Assert.Empty(store.OpenRangeCalls);
	}

	/// <summary>
	/// Verifies that multiple seek operations without any reads never perform
	/// any network requests.
	/// </summary>
	[Fact]
	public void Seek_MultipleOperations_DoNotOpenRange() {
		var store = new FakeStore();

		using var stream = new SeekableStream(
			store,
			"file",
			knownLength: 10000);

		stream.Seek(500, SeekOrigin.Begin);
		stream.Seek(-200, SeekOrigin.Current);
		stream.Seek(0, SeekOrigin.End);
		stream.Seek(100, SeekOrigin.Current);
		stream.Seek(2500, SeekOrigin.Begin);

		Assert.Empty(store.OpenRangeCalls);
	}

	/// <summary>
	/// Verifies that passing an invalid SeekOrigin value throws an
	/// ArgumentOutOfRangeException.
	/// </summary>
	[Fact]
	public void Seek_InvalidOrigin_Throws() {
		using var stream = new SeekableStream(new FakeStore(), "file");

		Assert.Throws<ArgumentOutOfRangeException>(() =>
			stream.Seek(0, (SeekOrigin)999));
	}
}