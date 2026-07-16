using FluentStorage.Streaming;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace FluentStorage.Tests.Streaming;

/// <summary>
/// Unit tests covering read operations after seeking.
/// </summary>
public sealed class SeekableStreamReadAfterSeekTests {

	/// <summary>
	/// Verifies that seeking backwards within the currently buffered region does
	/// not trigger another OpenRange request.
	/// </summary>
	[Fact]
	public void Read_SeekBackWithinBuffer_NoFetch() {
		byte[] data = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
		var store = new FakeStore(data);

		using var stream = new SeekableStream(store, "file", bufferSize: 64);

		byte[] buffer = new byte[32];

		stream.Read(buffer, 0, 32);
		Assert.Single(store.OpenRangeCalls);

		stream.Seek(10, SeekOrigin.Begin);
		stream.Read(buffer, 0, 10);

		Assert.Single(store.OpenRangeCalls);
	}

	/// <summary>
	/// Verifies that seeking backwards outside the buffered region causes a new
	/// buffer to be fetched on the next read.
	/// </summary>
	[Fact]
	public void Read_SeekBackOutsideBuffer_Fetches() {
		byte[] data = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
		var store = new FakeStore(data);

		using var stream = new SeekableStream(store, "file", bufferSize: 64);

		byte[] buffer = new byte[64];

		stream.Seek(128, SeekOrigin.Begin);
		stream.Read(buffer, 0, buffer.Length);

		Assert.Single(store.OpenRangeCalls);

		stream.Seek(0, SeekOrigin.Begin);
		stream.Read(buffer, 0, 1);

		Assert.Equal(2, store.OpenRangeCalls.Count);
	}

	/// <summary>
	/// Verifies that seeking forward within the currently buffered region does
	/// not trigger another OpenRange request.
	/// </summary>
	[Fact]
	public void Read_SeekForwardWithinBuffer_NoFetch() {
		byte[] data = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
		var store = new FakeStore(data);

		using var stream = new SeekableStream(store, "file", bufferSize: 64);

		byte[] buffer = new byte[32];

		stream.Read(buffer, 0, 32);

		Assert.Single(store.OpenRangeCalls);

		stream.Seek(40, SeekOrigin.Begin);
		stream.Read(buffer, 0, 8);

		Assert.Single(store.OpenRangeCalls);
	}

	/// <summary>
	/// Verifies that seeking forward outside the buffered region causes another
	/// OpenRange request on the next read.
	/// </summary>
	[Fact]
	public void Read_SeekForwardOutsideBuffer_Fetches() {
		byte[] data = Enumerable.Range(0, 512).Select(i => (byte)i).ToArray();
		var store = new FakeStore(data);

		using var stream = new SeekableStream(store, "file", bufferSize: 64);

		byte[] buffer = new byte[16];

		stream.Read(buffer, 0, buffer.Length);

		Assert.Single(store.OpenRangeCalls);

		stream.Seek(128, SeekOrigin.Begin);
		stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(2, store.OpenRangeCalls.Count);
	}

	/// <summary>
	/// Verifies that seeking back to the beginning after reading from a later
	/// buffer causes the first buffer to be fetched again.
	/// </summary>
	[Fact]
	public void Read_SeekToBeginning_Refetches() {
		byte[] data = Enumerable.Range(0, 512).Select(i => (byte)i).ToArray();
		var store = new FakeStore(data);

		using var stream = new SeekableStream(store, "file", bufferSize: 64);

		byte[] buffer = new byte[16];

		stream.Seek(256, SeekOrigin.Begin);
		stream.Read(buffer, 0, buffer.Length);

		Assert.Single(store.OpenRangeCalls);

		stream.Seek(0, SeekOrigin.Begin);
		stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(2, store.OpenRangeCalls.Count);
	}

	/// <summary>
	/// Verifies that reading after seeking to another location within the cached
	/// buffer does not perform another OpenRange request.
	/// </summary>
	[Fact]
	public void Read_SeekIntoCachedRegion_NoFetch() {
		byte[] data = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
		var store = new FakeStore(data);

		using var stream = new SeekableStream(store, "file", bufferSize: 64);

		byte[] buffer = new byte[16];

		stream.Read(buffer, 0, buffer.Length);
		stream.Seek(48, SeekOrigin.Begin);
		stream.Read(buffer, 0, 8);

		Assert.Single(store.OpenRangeCalls);
	}
}