using FluentStorage.Streaming;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FluentStorage.Tests.Streaming;

/// <summary>
/// Unit tests covering asynchronous read operations.
/// </summary>
public sealed class SeekableStreamReadAsyncTests {

	/// <summary>
	/// Verifies that requesting to read zero bytes asynchronously returns zero
	/// immediately and does not perform any OpenRange requests.
	/// </summary>
	[Fact]
	public async Task ReadAsync_ZeroCount() {
		var store = new FakeStore(new byte[100]);

		using var stream = new SeekableStream(store, "file");

		byte[] buffer = new byte[10];

		int read = await stream.ReadAsync(buffer, 0, 0);

		Assert.Equal(0, read);
		Assert.Empty(store.OpenRangeCalls);
		Assert.Equal(0, stream.Position);
	}

	/// <summary>
	/// Verifies that asynchronously reading a file smaller than the internal
	/// buffer returns the complete file.
	/// </summary>
	[Fact]
	public async Task ReadAsync_SmallFile() {
		byte[] data = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();
		var store = new FakeStore(data);

		using var stream = new SeekableStream(
			store,
			"file",
			bufferSize: 256);

		byte[] buffer = new byte[256];

		int read = await stream.ReadAsync(buffer, 0, buffer.Length);

		Assert.Equal(100, read);
		Assert.Equal(100, stream.Position);
		Assert.Equal(data, buffer[..100]);
		Assert.Single(store.OpenRangeCalls);
	}

	/// <summary>
	/// Verifies that asynchronously reading a file larger than the internal
	/// buffer loads multiple ranges and returns the complete file.
	/// </summary>
	[Fact]
	public async Task ReadAsync_LargeFile() {
		byte[] data = Enumerable.Range(0, 2048).Select(i => (byte)i).ToArray();
		var store = new FakeStore(data);

		using var stream = new SeekableStream(
			store,
			"file",
			bufferSize: 128);

		byte[] buffer = new byte[data.Length];

		int read = await stream.ReadAsync(buffer, 0, buffer.Length);

		Assert.Equal(data.Length, read);
		Assert.Equal(data.Length, stream.Position);
		Assert.Equal(data, buffer);
		Assert.True(store.OpenRangeCalls.Count > 1);
	}

	/// <summary>
	/// Verifies that attempting to read after the end of the object returns
	/// zero bytes.
	/// </summary>
	[Fact]
	public async Task ReadAsync_EOF() {
		byte[] data = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file");

		byte[] buffer = new byte[64];

		Assert.Equal(32, await stream.ReadAsync(buffer, 0, buffer.Length));
		Assert.Equal(0, await stream.ReadAsync(buffer, 0, buffer.Length));
		Assert.Equal(32, stream.Position);
	}

	/// <summary>
	/// Verifies that an OperationCanceledException thrown while opening a range
	/// is propagated without being wrapped.
	/// </summary>
	[Fact]
	public async Task ReadAsync_CancellationDuringOpenRange() {
		var store = new FakeStore {
			OpenRangeException = new OperationCanceledException()
		};

		using var stream = new SeekableStream(store, "file");

		await Assert.ThrowsAsync<OperationCanceledException>(async () =>
			await stream.ReadAsync(new byte[10], 0, 10));
	}

	/// <summary>
	/// Verifies that an OperationCanceledException thrown while reading the
	/// remote stream is propagated without being wrapped.
	/// </summary>
	[Fact]
	public async Task ReadAsync_CancellationDuringRemoteRead() {
		var store = new FakeStore(new byte[100]) {
			RemoteReadException = new OperationCanceledException()
		};

		using var stream = new SeekableStream(store, "file");

		await Assert.ThrowsAsync<OperationCanceledException>(async () =>
			await stream.ReadAsync(new byte[100], 0, 100));
	}
}