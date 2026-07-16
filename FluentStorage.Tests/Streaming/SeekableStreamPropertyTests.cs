using FluentStorage.Streaming;
using Xunit;

namespace FluentStorage.Tests.Streaming;

/// <summary>
/// Unit tests covering the Stream capability properties exposed by SeekableStream.
/// </summary>
public sealed class SeekableStreamPropertyTests {

	/// <summary>
	/// Verifies that the stream reports it supports reading.
	/// </summary>
	[Fact]
	public void CanRead_ReturnsTrue() {
		using var stream = new SeekableStream(new FakeStore(), "file");

		Assert.True(stream.CanRead);
	}

	/// <summary>
	/// Verifies that the stream reports it supports seeking.
	/// </summary>
	[Fact]
	public void CanSeek_ReturnsTrue() {
		using var stream = new SeekableStream(new FakeStore(), "file");

		Assert.True(stream.CanSeek);
	}

	/// <summary>
	/// Verifies that the stream reports it does not support writing.
	/// </summary>
	[Fact]
	public void CanWrite_ReturnsFalse() {
		using var stream = new SeekableStream(new FakeStore(), "file");

		Assert.False(stream.CanWrite);
	}

	/// <summary>
	/// Verifies that the stream reports it does not support timeouts.
	/// </summary>
	[Fact]
	public void CanTimeout_ReturnsFalse() {
		using var stream = new SeekableStream(new FakeStore(), "file");

		Assert.False(stream.CanTimeout);
	}
}