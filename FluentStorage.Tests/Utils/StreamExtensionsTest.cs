namespace FluentStorage.Tests.Utils {
	using FluentStorage.Utils.Extensions;
	using global::System.IO;
	using global::System.Text;
	using global::System.Threading.Tasks;
	using Xunit;

	public class StreamExtensionsTest {
		[Fact]
		public void MD5_hashes_a_stream_to_the_expected_value() {
			using var stream = new MemoryStream(
				Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog")
			);

			byte[] hash = stream.MD5();

			Assert.Equal("9e107d9d372bb6826bd81d3542a419d6", hash.ToHexString());
		}

		[Fact]
		public void MD5_can_be_called_concurrently() {
			// Regression test for https://github.com/robinrodricks/FluentStorage/issues/72: Stream.MD5()
			// used a shared static HashAlgorithm instance, which is not thread-safe. Each call gets its
			// own stream (a Stream is stateful); the shared hash was the bug. Two threads hash at once;
			// each loops enough times that the two reliably overlap inside the hash.
			byte[] content = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog");
			byte[] expected;
			using (var seed = new MemoryStream(content)) {
				expected = seed.MD5();
			}

			void Hash() {
				for (var j = 0; j < 2000; j++) {
					using var stream = new MemoryStream(content);
					Assert.Equal(expected, stream.MD5());
				}
			}

			Task.WaitAll(Task.Run(Hash), Task.Run(Hash));
		}
	}
}
