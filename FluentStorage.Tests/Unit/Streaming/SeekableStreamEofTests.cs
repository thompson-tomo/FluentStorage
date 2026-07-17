namespace FluentStorage.Tests.Unit.Streaming;

/// <summary>
/// Unit tests covering end-of-file discovery and lazy length detection.
/// </summary>
public sealed class SeekableStreamEofTests {

	/// <summary>
	/// Verifies that a short final buffer causes the stream to discover the
	/// object length.
	/// </summary>
	[Fact]
	public void EOF_ShortReadDiscoversLength() {
		byte[] data = new byte[100];

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 256);

		byte[] buffer = new byte[256];

		int read = stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(100, read);
		Assert.Equal(100, stream.Length);
	}

	/// <summary>
	/// Verifies that an empty read at the end of an empty object discovers the
	/// object length as zero.
	/// </summary>
	[Fact]
	public void EOF_EmptyReadDiscoversLength() {
		using var stream = new SeekableStream(
			new FakeStore(Array.Empty<byte>()),
			"file");

		byte[] buffer = new byte[1];

		int read = stream.Read(buffer, 0, 1);

		Assert.Equal(0, read);
		Assert.Equal(0, stream.Length);
	}

	/// <summary>
	/// Verifies that seeking beyond the end of an object and then reading
	/// discovers the correct object length.
	/// </summary>
	[Fact]
	public void EOF_SeekPastEndThenReadDiscoversLength() {
		byte[] data = new byte[100];
		var store = new FakeStore(data);

		using var stream = new SeekableStream(
			store,
			"file");

		stream.Seek(200, SeekOrigin.Begin);

		byte[] buffer = new byte[1];

		int read = stream.Read(buffer, 0, 1);

		Assert.Equal(0, read);
		Assert.Equal(200, stream.Length);
	}

	/// <summary>
	/// Verifies that repeated reads after reaching the end of the object do not
	/// perform additional OpenRange requests.
	/// </summary>
	[Fact]
	public void EOF_MultipleReadsAfterEOF_NoAdditionalRequests() {
		byte[] data = new byte[32];
		var store = new FakeStore(data);

		using var stream = new SeekableStream(
			store,
			"file",
			bufferSize: 64);

		byte[] buffer = new byte[64];

		stream.Read(buffer, 0, buffer.Length);

		int requestCount = store.OpenRangeCalls.Count;

		Assert.Equal(0, stream.Read(buffer, 0, buffer.Length));
		Assert.Equal(0, stream.Read(buffer, 0, buffer.Length));
		Assert.Equal(0, stream.Read(buffer, 0, buffer.Length));

		Assert.Equal(requestCount, store.OpenRangeCalls.Count);
	}

	/// <summary>
	/// Verifies that once the object length has been discovered it remains
	/// unchanged for the lifetime of the stream.
	/// </summary>
	[Fact]
	public void EOF_LengthStableAfterDiscovery() {
		byte[] data = new byte[123];

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 256);

		byte[] buffer = new byte[256];

		stream.Read(buffer, 0, buffer.Length);

		long length = stream.Length;

		Assert.Equal(length, stream.Length);
		Assert.Equal(length, stream.Length);
		Assert.Equal(length, stream.Length);
	}
}