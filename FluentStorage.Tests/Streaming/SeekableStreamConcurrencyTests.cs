using System;
using System.Linq;
using System.Threading.Tasks;
using FluentStorage.Streaming;
using Xunit;

namespace FluentStorage.Tests.Streaming;

/// <summary>
/// Unit tests covering concurrent access to SeekableStream.
/// </summary>
public sealed class SeekableStreamConcurrencyTests {

	/// <summary>
	/// Verifies that multiple concurrent reads complete successfully without
	/// throwing exceptions.
	/// </summary>
	[Fact]
	public async Task ConcurrentReads_DoNotThrow() {
		byte[] data = Enumerable.Range(0, 4096).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 128);

		var tasks = Enumerable.Range(0, 10)
			.Select(async _ => {
				byte[] buffer = new byte[64];
				await stream.ReadAsync(buffer, 0, buffer.Length);
			});

		await Task.WhenAll(tasks);
	}

	/// <summary>
	/// Verifies that concurrent reads serialize correctly and the final stream
	/// position equals the total number of bytes read.
	/// </summary>
	[Fact]
	public async Task ConcurrentReads_PositionCorrect() {
		byte[] data = Enumerable.Range(0, 4096).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 128);

		var tasks = Enumerable.Range(0, 20)
			.Select(async _ => {
				byte[] buffer = new byte[50];
				await stream.ReadAsync(buffer, 0, buffer.Length);
			});

		await Task.WhenAll(tasks);

		Assert.Equal(1000, stream.Position);
	}

	/// <summary>
	/// Verifies that concurrent reads eventually return all requested data
	/// without corrupting the stream state.
	/// </summary>
	[Fact]
	public async Task ConcurrentReads_AllComplete() {
		byte[] data = Enumerable.Range(0, 8192).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 256);

		var tasks = Enumerable.Range(0, 16)
			.Select(async _ => {
				byte[] buffer = new byte[128];
				return await stream.ReadAsync(buffer, 0, buffer.Length);
			});

		int[] results = await Task.WhenAll(tasks);

		Assert.All(results, bytesRead => Assert.Equal(128, bytesRead));
		Assert.Equal(2048, stream.Position);
	}

	/// <summary>
	/// Verifies that concurrent reads crossing buffer boundaries complete
	/// successfully.
	/// </summary>
	[Fact]
	public async Task ConcurrentReads_CrossBufferBoundaries() {
		byte[] data = Enumerable.Range(0, 8192).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file",
			bufferSize: 64);

		var tasks = Enumerable.Range(0, 8)
			.Select(async _ => {
				byte[] buffer = new byte[300];
				return await stream.ReadAsync(buffer, 0, buffer.Length);
			});

		int[] results = await Task.WhenAll(tasks);

		Assert.All(results, bytesRead => Assert.Equal(300, bytesRead));
		Assert.Equal(2400, stream.Position);
	}

	/// <summary>
	/// Verifies that concurrent reads do not deadlock.
	/// </summary>
	[Fact]
	public async Task ConcurrentReads_NoDeadlock() {
		byte[] data = Enumerable.Range(0, 4096).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file");

		var tasks = Enumerable.Range(0, 50)
			.Select(async _ => {
				byte[] buffer = new byte[16];
				await stream.ReadAsync(buffer, 0, buffer.Length);
			});

		var all = Task.WhenAll(tasks);

		var completed = await Task.WhenAny(all, Task.Delay(5000));

		Assert.Same(all, completed);

		await all;
	}

	/// <summary>
	/// Verifies that concurrent synchronous reads complete successfully.
	/// </summary>
	[Fact]
	public async Task ConcurrentSyncReads_DoNotThrow() {
		byte[] data = Enumerable.Range(0, 4096).Select(i => (byte)i).ToArray();

		using var stream = new SeekableStream(
			new FakeStore(data),
			"file");

		var tasks = Enumerable.Range(0, 16)
			.Select(_ => Task.Run(() => {
				byte[] buffer = new byte[32];
				stream.Read(buffer, 0, buffer.Length);
			}));

		await Task.WhenAll(tasks);

		Assert.Equal(512, stream.Position);
	}
}