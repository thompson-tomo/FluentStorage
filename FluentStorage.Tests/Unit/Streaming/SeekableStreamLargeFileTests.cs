namespace FluentStorage.Tests.Unit.Streaming;

/// <summary>
/// Unit tests covering large object and stress scenarios.
/// </summary>
public sealed class SeekableStreamLargeFileTests {

	/// <summary>
	/// Verifies that a large object can be read completely using many internal
	/// buffer loads.
	/// </summary>
	[Fact]
	public void LargeFile_ReadEntireObject() {
		byte[] data = Enumerable.Range(0, 1024 * 1024)
			.Select(i => (byte)i)
			.ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 4096);

		byte[] buffer = new byte[data.Length];

		int read = stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(data.Length, read);
		Assert.Equal(data, buffer);
		Assert.Equal(data.Length, stream.Position);
	}

	/// <summary>
	/// Verifies that repeatedly reading small blocks from a large object
	/// eventually reaches the end of the stream.
	/// </summary>
	[Fact]
	public void LargeFile_ManySmallReads() {
		byte[] data = Enumerable.Range(0, 1024 * 1024)
			.Select(i => (byte)i)
			.ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 2048);

		byte[] buffer = new byte[17];

		long total = 0;

		while (true) {
			int read = stream.Read(buffer, 0, buffer.Length);

			if (read == 0)
				break;

			total += read;
		}

		Assert.Equal(data.Length, total);
		Assert.Equal(data.Length, stream.Position);
	}

	/// <summary>
	/// Verifies that repeatedly seeking to distant locations and reading small
	/// amounts succeeds.
	/// </summary>
	[Fact]
	public void LargeFile_RandomAccess() {
		byte[] data = Enumerable.Range(0, 1024 * 1024)
			.Select(i => (byte)i)
			.ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 4096);

		byte[] buffer = new byte[32];

		for (int i = 0; i < 1000; i++) {
			long position = i * 997 % (data.Length - 32);

			stream.Seek(position, SeekOrigin.Begin);

			int read = stream.Read(buffer, 0, buffer.Length);

			Assert.Equal(32, read);

			for (int j = 0; j < read; j++)
				Assert.Equal(data[position + j], buffer[j]);
		}
	}

	/// <summary>
	/// Verifies that repeatedly reading the entire object from the beginning
	/// produces identical results every time.
	/// </summary>
	[Fact]
	public void LargeFile_MultiplePasses() {
		byte[] data = Enumerable.Range(0, 256 * 1024)
			.Select(i => (byte)i)
			.ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 4096);

		byte[] buffer = new byte[data.Length];

		for (int pass = 0; pass < 3; pass++) {

			stream.Seek(0, SeekOrigin.Begin);

			int read = stream.Read(buffer, 0, buffer.Length);

			Assert.Equal(data.Length, read);
			Assert.Equal(data, buffer);
		}
	}

	/// <summary>
	/// Verifies that reading one byte at a time from a large object eventually
	/// reaches the end of the stream.
	/// </summary>
	[Fact]
	public void LargeFile_OneByteReads() {
		byte[] data = Enumerable.Range(0, 128 * 1024)
			.Select(i => (byte)i)
			.ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 512);

		byte[] buffer = new byte[1];

		long total = 0;

		while (stream.Read(buffer, 0, 1) == 1)
			total++;

		Assert.Equal(data.Length, total);
		Assert.Equal(data.Length, stream.Position);
	}

	/// <summary>
	/// Verifies that a large read spanning many internal buffers returns the
	/// correct number of bytes.
	/// </summary>
	[Fact]
	public void LargeFile_VeryLargeRead() {
		byte[] data = Enumerable.Range(0, 2 * 1024 * 1024)
			.Select(i => (byte)i)
			.ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 8192);

		byte[] buffer = new byte[data.Length];

		int read = stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(data.Length, read);
		Assert.Equal(data, buffer);
		Assert.Equal(data.Length, stream.Position);
	}
}