using System;
using System.Linq;
using FluentStorage.Streaming;
using Xunit;

namespace FluentStorage.Tests.Streaming;

/// <summary>
/// Unit tests covering sequential synchronous read operations.
/// </summary>
public sealed class SeekableStreamSequentialReadTests {

	/// <summary>
	/// Verifies that repeatedly reading a single byte only fetches the first
	/// buffer once while the reads remain within the cached region.
	/// </summary>
	[Fact]
	public void Read_OneByteRepeatedly_UsesSingleFetch() {
		byte[] data = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
		var store = new FakeStore(data);

		using var stream = new SeekableStream(store, "file", bufferSize: 64);

		byte[] buffer = new byte[1];

		for (int i = 0; i < 32; i++) {
			int read = stream.Read(buffer, 0, 1);

			Assert.Equal(1, read);
			Assert.Equal(data[i], buffer[0]);
		}

		Assert.Single(store.OpenRangeCalls);
		Assert.Equal(32, stream.Position);
	}

	/// <summary>
	/// Verifies that reading exactly to the end of the current buffer does not
	/// require loading another buffer.
	/// </summary>
	[Fact]
	public void Read_BufferBoundary() {
		byte[] data = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
		var store = new FakeStore(data);

		using var stream = new SeekableStream(store, "file", bufferSize: 64);

		byte[] buffer = new byte[64];

		int read = stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(64, read);
		Assert.Single(store.OpenRangeCalls);
		Assert.Equal(64, stream.Position);
		Assert.Equal(data.Take(64), buffer);
	}

	/// <summary>
	/// Verifies that reading across a single buffer boundary loads exactly one
	/// additional buffer.
	/// </summary>
	[Fact]
	public void Read_CrossesSingleBoundary() {
		byte[] data = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
		var store = new FakeStore(data);

		using var stream = new SeekableStream(store, "file", bufferSize: 64);

		byte[] buffer = new byte[100];

		int read = stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(100, read);
		Assert.Equal(2, store.OpenRangeCalls.Count);
		Assert.Equal(100, stream.Position);
		Assert.Equal(data.Take(100), buffer);
	}

	/// <summary>
	/// Verifies that reading across several buffer boundaries loads one buffer
	/// per boundary crossed.
	/// </summary>
	[Fact]
	public void Read_CrossesMultipleBoundaries() {
		byte[] data = Enumerable.Range(0, 512).Select(i => (byte)i).ToArray();
		var store = new FakeStore(data);

		using var stream = new SeekableStream(store, "file", bufferSize: 64);

		byte[] buffer = new byte[300];

		int read = stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(300, read);
		Assert.Equal(5, store.OpenRangeCalls.Count);
		Assert.Equal(300, stream.Position);
		Assert.Equal(data.Take(300), buffer);
	}

	/// <summary>
	/// Verifies that reading exactly to the end of the object returns all
	/// remaining bytes.
	/// </summary>
	[Fact]
	public void Read_ExactEOF() {
		byte[] data = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 50);

		byte[] buffer = new byte[100];

		int read = stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(100, read);
		Assert.Equal(100, stream.Position);
		Assert.Equal(data, buffer);
	}

	/// <summary>
	/// Verifies that reading past the end of the object returns only the
	/// remaining bytes.
	/// </summary>
	[Fact]
	public void Read_PartialEOF() {
		byte[] data = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 64);

		byte[] buffer = new byte[200];

		int read = stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(100, read);
		Assert.Equal(100, stream.Position);
		Assert.Equal(data, buffer.Take(read));
	}

	/// <summary>
	/// Verifies that a single large read spanning many internal buffers returns
	/// the correct data and advances the position correctly.
	/// </summary>
	[Fact]
	public void Read_LargeRequestSpansManyBuffers() {
		byte[] data = Enumerable.Range(0, 4096).Select(i => (byte)i).ToArray();
		var store = new FakeStore(data);

		using var stream = new SeekableStream(store, "file", bufferSize: 128);

		byte[] buffer = new byte[data.Length];

		int read = stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(data.Length, read);
		Assert.Equal(data.Length, stream.Position);
		Assert.Equal(data, buffer);

		Assert.Equal(32, store.OpenRangeCalls.Count);
	}
}