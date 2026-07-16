using System;
using System.IO;
using FluentStorage.Streaming;
using Xunit;

namespace FluentStorage.Tests.Streaming;

/// <summary>
/// Unit tests covering Seek() using SeekOrigin.Current.
/// </summary>
public sealed class SeekableStreamSeekCurrentTests {

	/// <summary>
	/// Verifies that seeking forward relative to the current position updates
	/// the current position correctly.
	/// </summary>
	[Fact]
	public void SeekCurrent_Forward() {
		using var stream = new SeekableStream(new FakeStore(), "file");

		stream.Seek(100, SeekOrigin.Begin);

		long position = stream.Seek(50, SeekOrigin.Current);

		Assert.Equal(150, position);
		Assert.Equal(150, stream.Position);
	}

	/// <summary>
	/// Verifies that seeking backward relative to the current position updates
	/// the current position correctly.
	/// </summary>
	[Fact]
	public void SeekCurrent_Backward() {
		using var stream = new SeekableStream(new FakeStore(), "file");

		stream.Seek(250, SeekOrigin.Begin);

		long position = stream.Seek(-100, SeekOrigin.Current);

		Assert.Equal(150, position);
		Assert.Equal(150, stream.Position);
	}

	/// <summary>
	/// Verifies that seeking by zero leaves the current position unchanged.
	/// </summary>
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(1234)]
	[InlineData(987654321)]
	public void SeekCurrent_Zero(long startPosition) {
		using var stream = new SeekableStream(new FakeStore(), "file");

		stream.Seek(startPosition, SeekOrigin.Begin);

		long position = stream.Seek(0, SeekOrigin.Current);

		Assert.Equal(startPosition, position);
		Assert.Equal(startPosition, stream.Position);
	}

	/// <summary>
	/// Verifies that seeking beyond the end of a stream with a known length is
	/// allowed.
	/// </summary>
	[Fact]
	public void SeekCurrent_BeyondEOF() {
		using var stream = new SeekableStream(
			new FakeStore(),
			"file",
			knownLength: 1000);

		stream.Seek(900, SeekOrigin.Begin);

		long position = stream.Seek(500, SeekOrigin.Current);

		Assert.Equal(1400, position);
		Assert.Equal(1400, stream.Position);
	}

	/// <summary>
	/// Verifies that attempting to seek before the beginning of the stream
	/// relative to the current position throws an IOException.
	/// </summary>
	[Theory]
	[InlineData(0, -1)]
	[InlineData(100, -101)]
	[InlineData(500, -501)]
	[InlineData(1000, -1001)]
	public void SeekCurrent_BeforeBOF_Throws(long startPosition, long offset) {
		using var stream = new SeekableStream(new FakeStore(), "file");

		stream.Seek(startPosition, SeekOrigin.Begin);

		Assert.Throws<IOException>(() =>
			stream.Seek(offset, SeekOrigin.Current));
	}
}