using System;
using FluentStorage.Streaming;
using Xunit;

namespace FluentStorage.Tests.Streaming;

/// <summary>
/// Unit tests covering argument validation for Read().
/// </summary>
public sealed class SeekableStreamReadArgumentTests {

	/// <summary>
	/// Verifies that passing a null buffer throws an
	/// ArgumentNullException.
	/// </summary>
	[Fact]
	public void Read_NullBuffer_Throws() {
		using var stream = new SeekableStream(new FakeStore(), "file");

		Assert.Throws<ArgumentNullException>(() =>
			stream.Read(null!, 0, 1));
	}

	/// <summary>
	/// Verifies that a negative buffer offset throws an
	/// ArgumentOutOfRangeException.
	/// </summary>
	[Theory]
	[InlineData(-1)]
	[InlineData(-10)]
	[InlineData(int.MinValue)]
	public void Read_NegativeOffset_Throws(int offset) {
		using var stream = new SeekableStream(new FakeStore(), "file");

		Assert.Throws<ArgumentOutOfRangeException>(() =>
			stream.Read(new byte[10], offset, 1));
	}

	/// <summary>
	/// Verifies that a negative byte count throws an
	/// ArgumentOutOfRangeException.
	/// </summary>
	[Theory]
	[InlineData(-1)]
	[InlineData(-10)]
	[InlineData(int.MinValue)]
	public void Read_NegativeCount_Throws(int count) {
		using var stream = new SeekableStream(new FakeStore(), "file");

		Assert.Throws<ArgumentOutOfRangeException>(() =>
			stream.Read(new byte[10], 0, count));
	}

	/// <summary>
	/// Verifies that specifying an offset beyond the end of the destination
	/// buffer throws an ArgumentException.
	/// </summary>
	[Theory]
	[InlineData(11)]
	[InlineData(20)]
	[InlineData(int.MaxValue)]
	public void Read_OffsetTooLarge_Throws(int offset) {
		using var stream = new SeekableStream(new FakeStore(), "file");

		Assert.Throws<ArgumentException>(() =>
			stream.Read(new byte[10], offset, 0));
	}

	/// <summary>
	/// Verifies that requesting more bytes than fit in the destination buffer
	/// throws an ArgumentException.
	/// </summary>
	[Theory]
	[InlineData(11)]
	[InlineData(20)]
	[InlineData(100)]
	public void Read_CountTooLarge_Throws(int count) {
		using var stream = new SeekableStream(new FakeStore(), "file");

		Assert.Throws<ArgumentException>(() =>
			stream.Read(new byte[10], 0, count));
	}

	/// <summary>
	/// Verifies that an offset and count combination exceeding the destination
	/// buffer length throws an ArgumentException.
	/// </summary>
	[Theory]
	[InlineData(5, 6)]
	[InlineData(8, 3)]
	[InlineData(9, 2)]
	[InlineData(10, 1)]
	public void Read_InvalidOffsetCount_Throws(int offset, int count) {
		using var stream = new SeekableStream(new FakeStore(), "file");

		Assert.Throws<ArgumentException>(() =>
			stream.Read(new byte[10], offset, count));
	}

	/// <summary>
	/// Verifies that reading the entire destination buffer is permitted.
	/// </summary>
	[Fact]
	public void Read_EntireBuffer_DoesNotThrow() {
		using var stream = new SeekableStream(
			new FakeStore(new byte[10]),
			"file");

		byte[] buffer = new byte[10];

		var exception = Record.Exception(() =>
			stream.Read(buffer, 0, buffer.Length));

		Assert.Null(exception);
	}

	/// <summary>
	/// Verifies that reading zero bytes at the end of the destination buffer
	/// is permitted.
	/// </summary>
	[Fact]
	public void Read_ZeroCountAtBufferEnd_DoesNotThrow() {
		using var stream = new SeekableStream(new FakeStore(), "file");

		byte[] buffer = new byte[10];

		var exception = Record.Exception(() =>
			stream.Read(buffer, 10, 0));

		Assert.Null(exception);
	}
}