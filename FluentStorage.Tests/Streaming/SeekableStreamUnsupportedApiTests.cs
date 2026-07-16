using System;
using System.IO;
using System.Threading.Tasks;
using FluentStorage.Streaming;
using Xunit;

namespace FluentStorage.Tests.Streaming;

/// <summary>
/// Unit tests covering unsupported Stream operations.
/// </summary>
public sealed class SeekableStreamUnsupportedApiTests {

	/// <summary>
	/// Verifies that CanRead always returns true.
	/// </summary>
	[Fact]
	public void CanRead_IsTrue() {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file");

		Assert.True(stream.CanRead);
	}

	/// <summary>
	/// Verifies that CanSeek always returns true.
	/// </summary>
	[Fact]
	public void CanSeek_IsTrue() {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file");

		Assert.True(stream.CanSeek);
	}

	/// <summary>
	/// Verifies that CanWrite always returns false.
	/// </summary>
	[Fact]
	public void CanWrite_IsFalse() {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file");

		Assert.False(stream.CanWrite);
	}

	/// <summary>
	/// Verifies that CanTimeout always returns false.
	/// </summary>
	[Fact]
	public void CanTimeout_IsFalse() {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file");

		Assert.False(stream.CanTimeout);
	}

	/// <summary>
	/// Verifies that Write throws a NotSupportedException because the stream is
	/// read-only.
	/// </summary>
	[Fact]
	public void Write_ThrowsNotSupportedException() {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file");

		var ex = Assert.Throws<NotSupportedException>(() =>
			stream.Write(new byte[1], 0, 1));

		Assert.Contains("read-only", ex.Message, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Verifies that SetLength throws a NotSupportedException because the stream
	/// is read-only.
	/// </summary>
	[Fact]
	public void SetLength_ThrowsNotSupportedException() {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file");

		var ex = Assert.Throws<NotSupportedException>(() =>
			stream.SetLength(123));

		Assert.Contains("read-only", ex.Message, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Verifies that Flush completes successfully.
	/// </summary>
	[Fact]
	public void Flush_DoesNotThrow() {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file");

		var exception = Record.Exception(() => stream.Flush());

		Assert.Null(exception);
	}

	/// <summary>
	/// Verifies that FlushAsync completes successfully.
	/// </summary>
	[Fact]
	public async Task FlushAsync_DoesNotThrow() {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file");

		var exception = await Record.ExceptionAsync(async () =>
			await stream.FlushAsync());

		Assert.Null(exception);
	}

	/// <summary>
	/// Verifies that FlushAsync returns an already completed task.
	/// </summary>
	[Fact]
	public void FlushAsync_ReturnsCompletedTask() {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file");

		Task task = stream.FlushAsync();

		Assert.True(task.IsCompletedSuccessfully);
	}
}