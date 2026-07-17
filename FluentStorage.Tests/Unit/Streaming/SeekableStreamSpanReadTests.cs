namespace FluentStorage.Tests.Unit.Streaming;

/// <summary>
/// Unit tests covering the Span-based Read() overload.
/// </summary>
public sealed class SeekableStreamSpanReadTests {

	/// <summary>
	/// Verifies that reading into an empty span returns zero bytes and does not
	/// perform any OpenRange requests.
	/// </summary>
	[Fact]
	public void ReadSpan_Zero() {
		var store = new FakeStore(new byte[100]);

		using var stream = new SeekableStream(store, "file");

		Span<byte> buffer = Span<byte>.Empty;

		int read = stream.Read(buffer);

		Assert.Equal(0, read);
		Assert.Empty(store.OpenRangeCalls);
		Assert.Equal(0, stream.Position);
	}

	/// <summary>
	/// Verifies that reading a small object into a span returns all bytes
	/// correctly.
	/// </summary>
	[Fact]
	public void ReadSpan_Small() {
		byte[] data = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 256);

		Span<byte> buffer = stackalloc byte[256];

		int read = stream.Read(buffer);

		Assert.Equal(100, read);
		Assert.Equal(100, stream.Position);
		Assert.True(buffer[..100].SequenceEqual(data));
	}

	/// <summary>
	/// Verifies that reading a large object spanning multiple internal buffers
	/// returns all bytes correctly.
	/// </summary>
	[Fact]
	public void ReadSpan_Large() {
		byte[] data = Enumerable.Range(0, 4096).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 128);

		byte[] destination = new byte[data.Length];

		int read = stream.Read(destination.AsSpan());

		Assert.Equal(data.Length, read);
		Assert.Equal(data.Length, stream.Position);
		Assert.Equal(data, destination);
	}

	/// <summary>
	/// Verifies that attempting to read after reaching the end of the object
	/// returns zero bytes.
	/// </summary>
	[Fact]
	public void ReadSpan_EOF() {
		byte[] data = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file");

		byte[] destination = new byte[64];

		Assert.Equal(32, stream.Read(destination.AsSpan()));
		Assert.Equal(0, stream.Read(destination.AsSpan()));
		Assert.Equal(32, stream.Position);
	}

	/// <summary>
	/// Verifies that reading into a span across an internal buffer boundary
	/// returns the correct data.
	/// </summary>
	[Fact]
	public void ReadSpan_CrossBufferBoundary() {
		byte[] data = Enumerable.Range(0, 200).Select(i => (byte)i).ToArray();

		var store = new FakeStore(data);

		using var stream = new SeekableStream(
			store,
			"file",
			bufferSize: 64);

		byte[] destination = new byte[100];

		int read = stream.Read(destination.AsSpan());

		Assert.Equal(100, read);
		Assert.Equal(100, stream.Position);
		Assert.Equal(data.Take(100), destination);
		Assert.Equal(2, store.OpenRangeCalls.Count);
	}
}