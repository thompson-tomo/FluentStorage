using FluentStorage.Streaming;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace FluentStorage.Tests.Streaming;

/// <summary>
/// Unit tests covering stream disposal behaviour.
/// </summary>
public sealed class SeekableStreamDisposeTests {

	/// <summary>
	/// Verifies that disposing the stream multiple times does not throw an
	/// exception.
	/// </summary>
	[Fact]
	public void Dispose_MultipleTimes_DoesNotThrow() {
		var stream = new SeekableStream(
			new FakeStore(),
			"file");

		stream.Dispose();

		var exception = Record.Exception(() => stream.Dispose());

		Assert.Null(exception);
	}

	/// <summary>
	/// Verifies that reading after the stream has been disposed throws an
	/// ObjectDisposedException.
	/// </summary>
	[Fact]
	public void Dispose_Read_ThrowsObjectDisposedException() {
		var stream = new SeekableStream(
			new FakeStore(),
			"file");

		stream.Dispose();

		Assert.Throws<ObjectDisposedException>(() =>
			stream.Read(new byte[1], 0, 1));
	}

	/// <summary>
	/// Verifies that seeking after the stream has been disposed throws an
	/// ObjectDisposedException.
	/// </summary>
	[Fact]
	public void Dispose_Seek_ThrowsObjectDisposedException() {
		var stream = new SeekableStream(
			new FakeStore(),
			"file");

		stream.Dispose();

		Assert.Throws<ObjectDisposedException>(() =>
			stream.Seek(0, SeekOrigin.Begin));
	}

	/// <summary>
	/// Verifies that accessing the Position property after disposal throws an
	/// ObjectDisposedException.
	/// </summary>
	[Fact]
	public void Dispose_Position_ThrowsObjectDisposedException() {
		var stream = new SeekableStream(
			new FakeStore(),
			"file");

		stream.Dispose();

		Assert.Throws<ObjectDisposedException>(() => _ = stream.Position);
	}

	/// <summary>
	/// Verifies that setting the Position property after disposal throws an
	/// ObjectDisposedException.
	/// </summary>
	[Fact]
	public void Dispose_SetPosition_ThrowsObjectDisposedException() {
		var stream = new SeekableStream(
			new FakeStore(),
			"file");

		stream.Dispose();

		Assert.Throws<ObjectDisposedException>(() =>
			stream.Position = 10);
	}

	/// <summary>
	/// Verifies that accessing the Length property after disposal throws an
	/// ObjectDisposedException.
	/// </summary>
	[Fact]
	public void Dispose_Length_ThrowsObjectDisposedException() {
		var stream = new SeekableStream(
			new FakeStore(),
			"file",
			knownLength: 100);

		stream.Dispose();

		Assert.Throws<ObjectDisposedException>(() => _ = stream.Length);
	}

	/// <summary>
	/// Verifies that Flush() remains a harmless no-op after the stream has been
	/// disposed.
	/// </summary>
	[Fact]
	public void Dispose_Flush_DoesNotThrow() {
		var stream = new SeekableStream(
			new FakeStore(),
			"file");

		stream.Dispose();

		var exception = Record.Exception(() => stream.Flush());

		Assert.Null(exception);
	}

	/// <summary>
	/// Verifies that FlushAsync() remains a harmless no-op after the stream has
	/// been disposed.
	/// </summary>
	[Fact]
	public async Task Dispose_FlushAsync_DoesNotThrow() {
		var stream = new SeekableStream(
			new FakeStore(),
			"file");

		stream.Dispose();

		var exception = await Record.ExceptionAsync(async () =>
			await stream.FlushAsync());

		Assert.Null(exception);
	}
}