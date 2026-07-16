using FluentStorage.Streaming;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace FluentStorage.Tests.Streaming;

/// <summary>
/// Unit tests covering the internal read buffer and cache behaviour.
/// </summary>
public sealed class SeekableStreamBufferTests {

	/// <summary>
	/// Verifies that the first read loads the initial buffer from the store.
	/// </summary>
	[Fact]
	public void Buffer_FirstReadLoadsBuffer() {
		var store = new FakeStore(Enumerable.Range(0, 256).Select(i => (byte)i).ToArray());

		using var stream = new SeekableStream(store, "file", bufferSize: 64);

		stream.Read(new byte[1], 0, 1);

		Assert.Single(store.OpenRangeCalls);
		Assert.Equal(0, store.OpenRangeCalls[0].Offset);
		Assert.Equal(64, store.OpenRangeCalls[0].Length);
	}

	/// <summary>
	/// Verifies that subsequent reads entirely within the cached buffer do not
	/// perform another OpenRange request.
	/// </summary>
	[Fact]
	public void Buffer_SecondReadWithinBuffer_NoFetch() {
		var store = new FakeStore(Enumerable.Range(0, 256).Select(i => (byte)i).ToArray());

		using var stream = new SeekableStream(store, "file", bufferSize: 64);

		stream.Read(new byte[16], 0, 16);
		stream.Read(new byte[16], 0, 16);

		Assert.Single(store.OpenRangeCalls);
	}

	/// <summary>
	/// Verifies that reading beyond the cached buffer loads the next buffer.
	/// </summary>
	[Fact]
	public void Buffer_ReadPastBufferBoundary_LoadsNextBuffer() {
		var store = new FakeStore(Enumerable.Range(0, 256).Select(i => (byte)i).ToArray());

		using var stream = new SeekableStream(store, "file", bufferSize: 64);

		stream.Read(new byte[80], 0, 80);

		Assert.Equal(2, store.OpenRangeCalls.Count);
		Assert.Equal(0, store.OpenRangeCalls[0].Offset);
		Assert.Equal(64, store.OpenRangeCalls[1].Offset);
	}

	/// <summary>
	/// Verifies that seeking within the currently cached buffer does not cause
	/// another buffer fetch.
	/// </summary>
	[Fact]
	public void Buffer_SeekWithinBuffer_NoFetch() {
		var store = new FakeStore(Enumerable.Range(0, 256).Select(i => (byte)i).ToArray());

		using var stream = new SeekableStream(store, "file", bufferSize: 64);

		stream.Read(new byte[32], 0, 32);
		stream.Seek(10, SeekOrigin.Begin);
		stream.Read(new byte[8], 0, 8);

		Assert.Single(store.OpenRangeCalls);
	}

	/// <summary>
	/// Verifies that seeking outside the cached buffer causes the next read to
	/// fetch another buffer.
	/// </summary>
	[Fact]
	public void Buffer_SeekOutsideBuffer_Fetch() {
		var store = new FakeStore(Enumerable.Range(0, 512).Select(i => (byte)i).ToArray());

		using var stream = new SeekableStream(store, "file", bufferSize: 64);

		stream.Read(new byte[16], 0, 16);
		stream.Seek(128, SeekOrigin.Begin);
		stream.Read(new byte[1], 0, 1);

		Assert.Equal(2, store.OpenRangeCalls.Count);
		Assert.Equal(128, store.OpenRangeCalls[1].Offset);
	}

	/// <summary>
	/// Verifies that loading a different region replaces the previous cached
	/// buffer rather than retaining multiple buffers.
	/// </summary>
	[Fact]
	public void Buffer_ReplacesPreviousBuffer() {
		var store = new FakeStore(Enumerable.Range(0, 512).Select(i => (byte)i).ToArray());

		using var stream = new SeekableStream(store, "file", bufferSize: 64);

		stream.Read(new byte[1], 0, 1);
		stream.Seek(128, SeekOrigin.Begin);
		stream.Read(new byte[1], 0, 1);
		stream.Seek(0, SeekOrigin.Begin);
		stream.Read(new byte[1], 0, 1);

		Assert.Equal(3, store.OpenRangeCalls.Count);
	}

	/// <summary>
	/// Verifies that the stream never prefetches additional buffers beyond what
	/// is immediately required.
	/// </summary>
	[Fact]
	public void Buffer_DoesNotPrefetch() {
		var store = new FakeStore(Enumerable.Range(0, 512).Select(i => (byte)i).ToArray());

		using var stream = new SeekableStream(store, "file", bufferSize: 64);

		stream.Read(new byte[1], 0, 1);

		Assert.Single(store.OpenRangeCalls);
	}

	/// <summary>
	/// Verifies that partially consuming a buffer does not invalidate the cache.
	/// </summary>
	[Fact]
	public void Buffer_PartialRead_KeepsBuffer() {
		var store = new FakeStore(Enumerable.Range(0, 512).Select(i => (byte)i).ToArray());

		using var stream = new SeekableStream(store, "file", bufferSize: 64);

		stream.Read(new byte[10], 0, 10);
		stream.Read(new byte[20], 0, 20);
		stream.Read(new byte[30], 0, 30);

		Assert.Single(store.OpenRangeCalls);
	}

	/// <summary>
	/// Verifies that every OpenRange request starts exactly at the beginning of
	/// the required buffer.
	/// </summary>
	[Fact]
	public void Buffer_BufferStartCorrect() {
		var store = new FakeStore(Enumerable.Range(0, 512).Select(i => (byte)i).ToArray());

		using var stream = new SeekableStream(store, "file", bufferSize: 64);

		stream.Seek(150, SeekOrigin.Begin);
		stream.Read(new byte[1], 0, 1);

		Assert.Single(store.OpenRangeCalls);
		Assert.Equal(150, store.OpenRangeCalls[0].Offset);
	}

	/// <summary>
	/// Verifies that every OpenRange request asks for the configured buffer
	/// size.
	/// </summary>
	[Theory]
	[InlineData(1)]
	[InlineData(32)]
	[InlineData(64)]
	[InlineData(4096)]
	public void Buffer_BufferLengthCorrect(int bufferSize) {
		var store = new FakeStore(Enumerable.Range(0, 8192).Select(i => (byte)i).ToArray());

		using var stream = new SeekableStream(store, "file", bufferSize);

		stream.Read(new byte[1], 0, 1);

		Assert.Single(store.OpenRangeCalls);
		Assert.Equal(bufferSize, store.OpenRangeCalls[0].Length);
	}
}