namespace FluentStorage.Tests.Integration.Storage {
	public partial class IStoreTest {



		// ---------------------------------------------------------------------
		// Local Tree Helpers
		// ---------------------------------------------------------------------

		protected sealed record LocalFile(string RelativePath, int Size);

		protected static readonly LocalFile[] SmallTree =
		{
			new("root.txt",10),
			new("folder1/a.txt",25),
			new("folder1/b.txt",100),
			new("folder2/c.txt",500),
			new("folder2/sub/d.txt",1500),
		};

		protected static readonly LocalFile[] DeepTree =
		{
			new("a.txt",5),
			new("one/b.txt",15),
			new("one/two/c.txt",25),
			new("one/two/three/d.txt",35),
			new("one/two/three/four/e.txt",45),
			new("one/two/three/four/five/f.txt",55),
		};

		protected static readonly LocalFile[] UnicodeTree =
		{
			new("你好.txt",12),
			new("日本語/ファイル.txt",50),
			new("😀/नमस्ते.bin",400),
		};

		protected static readonly LocalFile[] EmptyFilesTree =
		{
			new("a.txt",0),
			new("b.txt",0),
			new("folder/c.txt",0),
		};

		protected static readonly LocalFile[] LargeTree =
		{
			new("one.bin",1024 * 1024),
			new("two.bin",1024 * 1024),
			new("sub/three.bin",1024 * 1024),
		};

		protected static void CreateLocalTree(
			string root,
			IEnumerable<LocalFile> files) {
			foreach (LocalFile file in files) {
				string full = Path.Combine(root, file.RelativePath.Replace('/', Path.DirectorySeparatorChar));

				Directory.CreateDirectory(Path.GetDirectoryName(full)!);

				File.WriteAllBytes(full, CreateBytes(file.Size));
			}
		}

		private static byte[] CreateBytes(int length) {
			byte[] data = new byte[length];

			for (int i = 0; i < length; i++)
				data[i] = (byte)(i % 251);

			return data;
		}

		// ---------------------------------------------------------------------
		// Remote Verification
		// ---------------------------------------------------------------------

		protected async Task AssertRemoteTree(
			string remoteRoot,
			IEnumerable<LocalFile> expected) {
			var files = expected.ToList();

			foreach (var file in files) {
				string remotePath = $"{remoteRoot}/{file.RelativePath.Replace('\\', '/')}";

				Assert.True(await _storage.ObjectExists(remotePath));

				Assert.Equal(file.Size, await _storage.GetObjectLength(remotePath));
			}

			var remoteFiles = await _storage.ListDirectory(remoteRoot, true);

			Assert.Equal(files.Count, remoteFiles.Count(x => x.IsFile));
		}

		protected async Task AssertRemoteDirectoriesExist(
			string remoteRoot,
			IEnumerable<LocalFile> expected) {
			var folders = expected
				.Select(x => Path.GetDirectoryName(x.RelativePath))
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct();

			foreach (string folder in folders) {
				string remote = $"{remoteRoot}/{folder.Replace('\\', '/')}";

				Assert.True(await _storage.DirectoryExists(remote));
			}
		}

		protected async Task AssertRemoteContainsExactly(string remoteRoot, int expectedFiles) {
			var list = await _storage.ListDirectory(remoteRoot, true);

			Assert.Equal(expectedFiles, list.Count(x => x.IsFile));
		}

		// ---------------------------------------------------------------------
		// Local Verification
		// ---------------------------------------------------------------------

		protected static void AssertLocalTree(
			string root,
			IEnumerable<LocalFile> expected) {
			var files = expected.ToList();

			foreach (var file in files) {
				string full = Path.Combine(root, file.RelativePath.Replace('/', Path.DirectorySeparatorChar));

				Assert.True(File.Exists(full));

				Assert.Equal(file.Size, new FileInfo(full).Length);
			}

			Assert.Equal(files.Count, Directory.GetFiles(root, "*", SearchOption.AllDirectories).Length);
		}

		// ---------------------------------------------------------------------
		// Progress Helpers
		// ---------------------------------------------------------------------

		protected sealed class ProgressRecorder {
			public List<StorageProgress> Items { get; } = new();

			public void Report(StorageProgress progress) {
				lock (Items) {
					Items.Add(progress);
				}
			}

			public int Count => Items.Count;

			public int SuccessCount =>
				Items.Count(x => x.Error == null);

			public int FailureCount =>
				Items.Count(x => x.Error != null);
		}

		// ---------------------------------------------------------------------
		// Convenience Helpers
		// ---------------------------------------------------------------------

		protected async Task UploadTree(
			IEnumerable<LocalFile> tree,
			string localRoot,
			string remoteRoot,
			StorageExistsMode mode = StorageExistsMode.Skip,
			ProgressRecorder recorder = null) {
			CreateLocalTree(localRoot, tree);

			await _storage.UploadDirectory(localRoot, remoteRoot, mode, recorder != null ? recorder.Report : null);
		}

		protected async Task DownloadTree(
			string remoteRoot,
			string localRoot,
			StorageExistsMode mode = StorageExistsMode.Skip,
			ProgressRecorder recorder = null) {
			await _storage.DownloadDirectory(remoteRoot, localRoot, mode, recorder != null ? recorder.Report : null);
		}

	}
}
