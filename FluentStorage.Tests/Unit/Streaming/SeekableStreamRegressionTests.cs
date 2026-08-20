namespace FluentStorage.Tests.Unit.Streaming;

/// <summary>
/// Unit tests covering miscellaneous edge cases and regression scenarios.
/// </summary>
public sealed class SeekableStreamRegressionTests {

	/// <summary>
	/// Verifies that reading exactly the final byte of an object succeeds and
	/// advances the position to the end of the stream.
	/// </summary>
	[Fact]
	public void Regression_ReadLastByte() {
		byte[] data = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file");

		stream.Seek(99, SeekOrigin.Begin);

		byte[] buffer = new byte[1];

		int read = stream.Read(buffer, 0, 1);

		Assert.Equal(1, read);
		Assert.Equal(data[99], buffer[0]);
		Assert.Equal(100, stream.Position);
	}

	/// <summary>
	/// Verifies that reading immediately after the final byte returns zero.
	/// </summary>
	[Fact]
	public void Regression_ReadImmediatelyAfterLastByte() {
		byte[] data = new byte[100];

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file");

		stream.Seek(100, SeekOrigin.Begin);

		Assert.Equal(0, stream.Read(new byte[1], 0, 1));
	}

	/// <summary>
	/// Verifies that repeatedly seeking to the same position behaves
	/// consistently.
	/// </summary>
	[Fact]
	public void Regression_RepeatedSeekSamePosition() {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file");

		for (int i = 0; i < 100; i++)
			Assert.Equal(123, stream.Seek(123, SeekOrigin.Begin));

		Assert.Equal(123, stream.Position);
	}

	/// <summary>
	/// Verifies that repeatedly reading after reaching the end of the stream
	/// always returns zero.
	/// </summary>
	[Fact]
	public void Regression_RepeatedEOFReads() {
		byte[] data = new byte[10];

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file");

		byte[] buffer = new byte[20];

		stream.Read(buffer, 0, buffer.Length);

		for (int i = 0; i < 100; i++)
			Assert.Equal(0, stream.Read(buffer, 0, buffer.Length));
	}

	/// <summary>
	/// Verifies that seeking to the current position does not change the stream
	/// state.
	/// </summary>
	[Fact]
	public void Regression_SeekCurrentZero() {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file");

		stream.Seek(500, SeekOrigin.Begin);

		long position = stream.Seek(0, SeekOrigin.Current);

		Assert.Equal(500, position);
		Assert.Equal(500, stream.Position);
	}

	/// <summary>
	/// Verifies that seeking to the beginning repeatedly does not affect
	/// subsequent reads.
	/// </summary>
	[Fact]
	public void Regression_RepeatedSeekBeginning() {
		byte[] data = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file");

		byte[] buffer = new byte[8];

		for (int i = 0; i < 10; i++) {
			stream.Seek(0, SeekOrigin.Begin);

			int read = stream.Read(buffer, 0, buffer.Length);

			Assert.Equal(8, read);
			Assert.Equal(data.Take(8), buffer);
		}
	}

	/// <summary>
	/// Verifies that alternating between nearby seek positions returns the
	/// correct data every time.
	/// </summary>
	[Fact]
	public void Regression_AlternateNearbySeeks() {
		byte[] data = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 64);

		byte[] buffer = new byte[1];

		for (int i = 0; i < 100; i++) {

			stream.Seek(20, SeekOrigin.Begin);
			Assert.Equal(1, stream.Read(buffer, 0, 1));
			Assert.Equal(data[20], buffer[0]);

			stream.Seek(21, SeekOrigin.Begin);
			Assert.Equal(1, stream.Read(buffer, 0, 1));
			Assert.Equal(data[21], buffer[0]);
		}
	}

	/// <summary>
	/// Verifies that alternating between distant seek positions always returns
	/// the expected data.
	/// </summary>
	[Fact]
	public void Regression_AlternateDistantSeeks() {
		byte[] data = Enumerable.Range(0, 1024).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 64);

		byte[] buffer = new byte[1];

		for (int i = 0; i < 50; i++) {

			stream.Seek(10, SeekOrigin.Begin);
			stream.Read(buffer, 0, 1);
			Assert.Equal(data[10], buffer[0]);

			stream.Seek(900, SeekOrigin.Begin);
			stream.Read(buffer, 0, 1);
			Assert.Equal(data[900], buffer[0]);
		}
	}

	/// <summary>
	/// Verifies that a read spanning exactly one internal buffer returns the
	/// expected number of bytes.
	/// </summary>
	[Fact]
	public void Regression_ReadExactlyOneBuffer() {
		byte[] data = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 64);

		byte[] buffer = new byte[64];

		int read = stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(64, read);
		Assert.Equal(data, buffer);
	}

	/// <summary>
	/// Verifies that reading exactly to a buffer boundary and then reading one
	/// more byte succeeds.
	/// </summary>
	[Fact]
	public void Regression_BufferBoundaryTransition() {
		byte[] data = Enumerable.Range(0, 128).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 64);

		byte[] buffer = new byte[64];

		Assert.Equal(64, stream.Read(buffer, 0, 64));

		byte[] next = new byte[1];

		Assert.Equal(1, stream.Read(next, 0, 1));
		Assert.Equal(data[64], next[0]);
	}


	/// <summary>
	/// Verifies that OpenRange receives the correct object path.
	/// </summary>
	[Fact]
	public void OpenRange_PathPassedCorrectly() {
		var store = new FakeStore(new byte[10]);

		using var stream = new SeekableStream(store, "abc/file.bin");

		stream.Read(new byte[1], 0, 1);

		Assert.Equal("abc/file.bin", store.OpenRangeCalls.Single().Path);
	}

	/// <summary>
	/// Verifies that an exception while opening a range does not change the
	/// current stream position.
	/// </summary>
	[Fact]
	public void Position_UnchangedAfterOpenRangeFailure() {
		var store = new FakeStore {
			OpenRangeException = new Exception()
		};

		using var stream = new SeekableStream(store, "file");

		stream.Seek(100, SeekOrigin.Begin);

		Assert.Throws<IOException>(() => stream.Read(new byte[1], 0, 1));

		Assert.Equal(100, stream.Position);
	}

	/// <summary>
	/// Verifies that an exception while reading the remote stream does not
	/// change the current stream position.
	/// </summary>
	[Fact]
	public void Position_UnchangedAfterRemoteReadFailure() {
		var store = new FakeStore(new byte[100]) {
			RemoteReadException = new Exception()
		};

		using var stream = new SeekableStream(store, "file");

		Assert.Throws<IOException>(() => stream.Read(new byte[10], 0, 10));

		Assert.Equal(0, stream.Position);
	}

	/// <summary>
	/// Verifies that a failed seek operation leaves the current position
	/// unchanged.
	/// </summary>
	[Fact]
	public void Position_UnchangedAfterFailedSeek() {
		using var stream = new SeekableStream(new FakeStore(), "file");

		stream.Seek(50, SeekOrigin.Begin);

		Assert.Throws<IOException>(() => stream.Seek(-100, SeekOrigin.Current));

		Assert.Equal(50, stream.Position);
	}

	/// <summary>
	/// Verifies correct operation when the internal buffer size is one byte.
	/// </summary>
	[Fact]
	public void BufferSize_One() {
		byte[] data = Enumerable.Range(0, 20).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 1);

		byte[] buffer = new byte[20];

		int read = stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(20, read);
		Assert.Equal(data, buffer);
	}

	/// <summary>
	/// Verifies correct operation when the object contains a single byte.
	/// </summary>
	[Fact]
	public void FileSize_OneByte() {
		byte[] data = { 123 };

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 64);

		byte[] buffer = new byte[10];

		Assert.Equal(1, stream.Read(buffer, 0, buffer.Length));
		Assert.Equal(123, buffer[0]);
		Assert.Equal(0, stream.Read(buffer, 0, buffer.Length));
	}

	/// <summary>
	/// Verifies that reading the same region twice returns identical data.
	/// </summary>
	[Fact]
	public void ReadSameRegionTwice() {
		byte[] data = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(new FakeStore(data), "file");

		byte[] first = new byte[32];
		byte[] second = new byte[32];

		stream.Seek(100, SeekOrigin.Begin);
		stream.Read(first, 0, first.Length);

		stream.Seek(100, SeekOrigin.Begin);
		stream.Read(second, 0, second.Length);

		Assert.Equal(first, second);
	}

	/// <summary>
	/// Verifies that CanRead, CanSeek and CanWrite remain stable after disposal.
	/// </summary>
	[Fact]
	public void StreamCapabilitiesAfterDispose() {
		var stream = new SeekableStream(new FakeStore(), "file");

		stream.Dispose();

		Assert.True(stream.CanRead);
		Assert.True(stream.CanSeek);
		Assert.False(stream.CanWrite);
		Assert.False(stream.CanTimeout);
	}

	/// <summary>
	/// Verifies that an empty object path is accepted.
	/// </summary>
	[Fact]
	public void Constructor_EmptyPath() {
		using var stream = new SeekableStream(
			new FakeStore(),
			string.Empty);

		Assert.Equal(0, stream.Position);
	}

}