using FluentStorage.Streaming;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace FluentStorage.Tests.Streaming;

/// <summary>
/// Unit tests covering basic synchronous read operations.
/// </summary>
public sealed class SeekableStreamReadBasicTests {

	/// <summary>
	/// Verifies that requesting to read zero bytes returns zero immediately and
	/// does not perform any OpenRange requests.
	/// </summary>
	[Fact]
	public void Read_ZeroCount_ReturnsZero() {
		var store = new FakeStore(new byte[100]);

		using var stream = new SeekableStream(store, "file");

		byte[] buffer = new byte[10];

		int read = stream.Read(buffer, 0, 0);

		Assert.Equal(0, read);
		Assert.Empty(store.OpenRangeCalls);
		Assert.Equal(0, stream.Position);
	}

	/// <summary>
	/// Verifies that reading a file smaller than the buffer size returns the
	/// complete file and discovers the object length.
	/// </summary>
	[Fact]
	public void Read_SmallFile() {
		byte[] data = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();
		var store = new FakeStore(data);

		using var stream = new SeekableStream(
			store,
			"file",
			bufferSize: 256);

		byte[] buffer = new byte[256];

		int read = stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(100, read);
		Assert.Equal(100, stream.Position);
		Assert.Equal(100, stream.Length);

		Assert.Single(store.OpenRangeCalls);
		Assert.Equal(data, buffer[..100]);
	}

	/// <summary>
	/// Verifies that reading a file whose size exactly matches the buffer size
	/// returns the complete file.
	/// </summary>
	[Fact]
	public void Read_FileEqualsBuffer() {
		byte[] data = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
		var store = new FakeStore(data);

		using var stream = new SeekableStream(
			store,
			"file",
			bufferSize: 256);

		byte[] buffer = new byte[256];

		int read = stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(256, read);
		Assert.Equal(256, stream.Position);

		Assert.Single(store.OpenRangeCalls);
		Assert.Equal(data, buffer);
	}

	/// <summary>
	/// Verifies that reading a file larger than the internal buffer succeeds
	/// across multiple buffer loads.
	/// </summary>
	[Fact]
	public void Read_FileLargerThanBuffer() {
		byte[] data = Enumerable.Range(0, 1000).Select(i => (byte)i).ToArray();
		var store = new FakeStore(data);

		using var stream = new SeekableStream(
			store,
			"file",
			bufferSize: 128);

		byte[] buffer = new byte[data.Length];

		int read = stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(data.Length, read);
		Assert.Equal(data.Length, stream.Position);
		Assert.Equal(data, buffer);

		Assert.True(store.OpenRangeCalls.Count > 1);
	}

	/// <summary>
	/// Verifies that reading the entire file in a single request returns all
	/// bytes correctly.
	/// </summary>
	[Fact]
	public void Read_EntireFile() {
		byte[] data = Enumerable.Range(0, 512).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file");

		byte[] buffer = new byte[512];

		int read = stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(512, read);
		Assert.Equal(data, buffer);
	}

	/// <summary>
	/// Verifies that attempting to read after reaching the end of the stream
	/// returns zero.
	/// </summary>
	[Fact]
	public void Read_AfterEOF_ReturnsZero() {
		byte[] data = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file");

		byte[] buffer = new byte[64];

		Assert.Equal(32, stream.Read(buffer, 0, buffer.Length));
		Assert.Equal(0, stream.Read(buffer, 0, buffer.Length));
		Assert.Equal(32, stream.Position);
	}

	/// <summary>
	/// Verifies that reading after seeking beyond the end of a stream with a
	/// known length immediately returns zero.
	/// </summary>
	[Fact]
	public void Read_AfterSeekPastEOF_ReturnsZero() {
		using var stream = new SeekableStream(
			new FakeStore(new byte[100]),
			"file",
			knownLength: 100);

		stream.Seek(1000, SeekOrigin.Begin);

		byte[] buffer = new byte[10];

		int read = stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(0, read);
		Assert.Equal(1000, stream.Position);
	}

	/// <summary>
	/// Verifies that attempting to read after the stream has been disposed
	/// throws an ObjectDisposedException.
	/// </summary>
	[Fact]
	public void Read_AfterDispose_Throws() {
		var stream = new SeekableStream(
			new FakeStore(),
			"file");

		stream.Dispose();

		Assert.Throws<ObjectDisposedException>(() =>
			stream.Read(new byte[1], 0, 1));
	}
}