
namespace FluentStorage.Tests.Integration.Storage {

	/// <summary>
	/// Massive test case suite to test object and directory manipulation for the given provider.
	/// Should work on disk providers and cloud storage providers.
	/// </summary>
	[Trait("Category", "Blobs")]
	public abstract class IStoreTest : IAsyncLifetime {
		private readonly IStore _storage;
		private readonly string _blobPrefix;
		private readonly IStoreFixture _fixture;

		public IStoreTest(IStoreFixture fixture) {
			_storage = fixture.Storage;
			_blobPrefix = fixture.BlobPrefix;
			_fixture = fixture;
		}

		public Task InitializeAsync() {
			return _fixture.InitAsync();
		}

		public Task DisposeAsync() {
			return _fixture.DisposeAsync();
		}

		private async Task CreateText(string path, string text = null) {
			await _storage.SetText(path, text ?? Guid.NewGuid().ToString());
		}

		private static string RandomName()
			=> Guid.NewGuid().ToString("N");

		private static string RandomFolder()
			=> $"tests/{Guid.NewGuid():N}";

		private static string RandomFile()
			=> $"tests/{Guid.NewGuid():N}.txt";

		private async Task<string> GetRandomStreamIdAsync(string prefix = null) {
			string id = RandomBlobPath();
			if (prefix != null)
				id = prefix + "/" + id;

			using (Stream s = "kjhlkhlkhlkhlkh".ToMemoryStream()) {
				await _storage.SetObject(id, s);
			}

			return id;
		}

		[Fact]
		public async Task List_All_DoesntCrash() {
			await _storage.ListObjects();
		}

		[Fact]
		public async Task List_RootFolder_HasAtLeastOne() {
			string targetId = RandomBlobPath();

			await _storage.SetText(targetId, "test");

			List<StoreObject> rootContent = await _storage.ListObjects();

			Assert.NotEmpty(rootContent);
		}

		[Fact]
		public async Task List_ByFilePrefix_Filtered() {
			string prefix = RandomGenerator.RandomString;

			int countBefore = (await _storage.ListObjects(new StorageListOptions { FolderPath = _blobPrefix, FilePrefix = prefix })).Count;

			string id1 = RandomBlobPath(prefix);
			string id2 = RandomBlobPath(prefix);
			string id3 = RandomBlobPath();

			await _storage.SetText(id1, RandomGenerator.RandomString);
			await _storage.SetText(id2, RandomGenerator.RandomString);
			await _storage.SetText(id3, RandomGenerator.RandomString);

			List<StoreObject> items = (await _storage.ListObjects(new StorageListOptions { FolderPath = _blobPrefix, FilePrefix = prefix }));
			Assert.Equal(2 + countBefore, items.Count); //2 files + containing folder
		}

		[Fact]
		public async Task List_FilesInFolder_NonRecursive() {
			string id = RandomBlobPath();

			await _storage.SetText(id, RandomGenerator.RandomString);

			List<StoreObject> items = (await _storage.ListObjects(new StorageListOptions { FolderPath = _blobPrefix, Recurse = false })).ToList();

			Assert.True(items.Count > 0);

			StoreObject tid = items.FirstOrDefault(i => i.FullPath == id);
			Assert.NotNull(tid);
		}

		[Fact]
		public async Task List_FilesInFolder_Recursive() {
			string folderPath = RandomBlobPath();
			string id1 = StoragePath.Combine(folderPath, "1.txt");
			string id2 = StoragePath.Combine(folderPath, "sub", "2.txt");
			string id3 = StoragePath.Combine(folderPath, "sub", "3.txt");

			try {
				await _storage.SetText(id1, RandomGenerator.RandomString);
				await _storage.SetText(id2, RandomGenerator.RandomString);
				await _storage.SetText(id3, RandomGenerator.RandomString);

				List<StoreObject> items = await _storage.ListDirectory(folderPath, true);
				Assert.Equal(4, items.Count); //1.txt + sub (folder) + 2.txt + 3.txt

			}
			catch (NotSupportedException) {
				//it ok for providers not to support hierarchy
			}
		}

		[Fact]
		public async Task List_InNonExistingFolder_EmptyCollection() {
			IEnumerable<StoreObject> objects = await _storage.ListObjects(new StorageListOptions { FolderPath = RandomBlobPath() });

			Assert.NotNull(objects);
			Assert.True(objects.Count() == 0);
		}

		[Fact]
		public async Task List_FilesInNonExistingFolder_EmptyCollection() {
			IEnumerable<StoreObject> objects = await (_storage as StoreBase).ListFileObjects(new StorageListOptions { FolderPath = RandomBlobPath() });

			Assert.NotNull(objects);
			Assert.True(objects.Count() == 0);
		}

		[Fact]
		public async Task List_VeryLongPrefix_NoResultsNoCrash() {
			await Assert.ThrowsAsync<ArgumentException>(async () => await _storage.ListObjects(new StorageListOptions { FilePrefix = RandomGenerator.GetRandomString(100000, false) }));
		}

		[Fact]
		public async Task List_limited_number_of_results() {
			string prefix = RandomGenerator.RandomString;
			string id1 = RandomBlobPath(prefix);
			string id2 = RandomBlobPath(prefix);
			await _storage.SetText(id1, RandomGenerator.RandomString);
			await _storage.SetText(id2, RandomGenerator.RandomString);

			int countAll = (await (_storage as StoreBase).ListFileObjects(new StorageListOptions { FolderPath = _blobPrefix, FilePrefix = prefix })).Count;
			int countOne = (await _storage.ListObjects(new StorageListOptions { FolderPath = _blobPrefix, FilePrefix = prefix, MaxResults = 1 })).Count;

			Assert.Equal(2, countAll);
			Assert.Equal(1, countOne);
		}

		[Fact]
		public async Task List_with_browsefilter_calls_filter() {
			string id1 = RandomBlobPath();
			string id2 = RandomBlobPath();
			await _storage.SetText(id1, RandomGenerator.RandomString);
			await _storage.SetText(id2, RandomGenerator.RandomString);

			//dump compare
			List<StoreObject> files = await (_storage as StoreBase).ListFileObjects(new StorageListOptions {
				FolderPath = _blobPrefix,
				Recurse = true
			});
			Assert.Contains(files, f => f.FullPath == id1 && f.Type == StorageObjectType.File);

			//server-side filtering
			files = await (_storage as StoreBase).ListFileObjects(new StorageListOptions {
				FolderPath = _blobPrefix,
				Recurse = true,
				BrowseFilter = id => (id.Type != StorageObjectType.File || id.FullPath == id1)
			});


			Assert.Single(files);
			Assert.Equal(id1, files.First().FullPath);
		}

		//[Fact]
		public async Task List_large_number_of_results() {
			const int count = 500;
			//arrange

			//something like FTP doesn't support multiple connections, however this should be implemented in FTP provider itself

			for (int it = 0; it < 50; it++) {
				await Task.WhenAll(Enumerable.Range(0, 10).Select(i => _storage.SetText(RandomBlobPath(), "123")));
			}

			//act
			List<StoreObject> blobs = await _storage.ListDirectory(folderPath: _blobPrefix);

			//assert
			Assert.True(blobs.Count >= count, $"expected over {count}, but received only {blobs.Count}");
		}

		[Fact]
		public async Task List_folder_nonrecursively_no_children() {
			try {
				string sub = RandomBlobPath() + "/";

				await _storage.SetText(sub + "one.txt", "test");
				await _storage.SetText(sub + "sub/two.txt", "test");

				List<StoreObject> subItems = await _storage.ListDirectory(sub, false);
				Assert.Equal(2, subItems.Count);


				Assert.Contains(new StoreObject(sub + "one.txt"), subItems);
				Assert.Contains(new StoreObject(sub + "sub", StorageObjectType.Folder), subItems);
			}
			catch (NotSupportedException) {
				//hierarchy not supported
			}
		}

		[Fact]
		public async Task GetBlob_for_one_file_succeeds() {
			string content = RandomGenerator.GetRandomString(1000, false);
			string id = RandomBlobPath();

			await _storage.SetText(id, content);

			StoreObject meta = await _storage.GetObjectInfo(id);

			long size = Encoding.UTF8.GetBytes(content).Length;
			string md5 = content.MD5();

			if (meta.Size != null)
				Assert.Equal(size, meta.Size);
			if (meta.MD5 != null)
				Assert.Equal(md5, meta.MD5);
			if (meta.DateModified != null)
				Assert.Equal(DateTime.UtcNow.RoundToDay(), meta.DateModified.Value.DateTime.RoundToDay());
		}

		[Fact]
		public async Task GetBlob_doesnt_exist_returns_null() {
			string id = RandomBlobPath();

			StoreObject meta = (await _storage.GetObjectsInfo(new[] { id })).First();

			Assert.Null(meta);
		}

		[Fact]
		public async Task GetBlob_Root_doesnt_exist_returns_null() {
			string id = "/" + Guid.NewGuid().ToString();
			//string id = "test";

			Assert.Null(await _storage.GetObjectInfo(id));
		}

		[Fact]
		public async Task GetBlob_Root_valid_returns_some() {
			string id = RandomBlobPath();

			string root = StoragePath.Split(id)[0];

			try {
				StoreObject rb = await _storage.GetObjectInfo(root);
			}
			catch (NotSupportedException) {

			}
		}


		[Fact]
		public async Task Open_doesnt_exist_returns_null() {
			string id = RandomBlobPath();

			Assert.Null(await _storage.OpenRead(id));
		}

		[Fact]
		public async Task Open_blob_exists_returns_stream() {
			string existingBlobPath = $"{Guid.NewGuid()}/existing-blob.txt";

			await _storage.SetText(existingBlobPath, "Hello, Blob!");

			var result = await _storage.OpenRead(existingBlobPath);
			Assert.NotNull(result);

			using var reader = new StreamReader(result);
			string content = await reader.ReadToEndAsync();
			Assert.Equal("Hello, Blob!", content);
		}


		[Fact]
		public async Task Open_empty_blob_returns_empty_stream() {
			string emptyBlobPath = $"{Guid.NewGuid()}/empty-blob.txt";

			await _storage.SetObject(emptyBlobPath, new MemoryStream(new byte[0]));
			Stream result = await _storage.OpenRead(emptyBlobPath);

			Assert.NotNull(result);
			Assert.Equal(0, result.Length);
		}

		[Fact]
		public async Task Open_copy_to_memory_stream_succeeds() {
			string id = await GetRandomStreamIdAsync();
			IStore ms = StorageFactory.InMemory();

			//if this doesn't crash it means the returned stream is compatible with usual .net streaming
			await _storage.CopyObjectTo(id, ms, id);
		}

		[Fact]
		public async Task Write_with_writeasync_succeeds() {
			string id = RandomBlobPath();
			byte[] data = Encoding.UTF8.GetBytes("oh my");

			await _storage.SetObject(id, new MemoryStream(data));

			//read and check
			string result = await _storage.GetText(id);
			Assert.Equal("oh my", result);

			// length
			var len = await _storage.GetObjectLength(id);
			Assert.Equal(data.Length, len);
		}

		[Fact]
		public async Task Write_nullDataStream_argumentnullexception() {
			await Assert.ThrowsAsync<ArgumentNullException>(() => _storage.SetObject(RandomBlobPath(), (Stream)null, false));
		}

		[Fact]
		public async Task Write_non_seekable_stream_succeeds() {
			string s = "test content";
			string id = RandomBlobPath();
			var data = Encoding.UTF8.GetBytes(s);

			var nonSeekable = new NonSeekableStream(new MemoryStream(data));

			await _storage.SetObject(id, nonSeekable);

			// check content
			Assert.Equal(s, await _storage.GetText(id));

			// check length
			var len = await _storage.GetObjectLength(id);
			Assert.Equal(data.Length, len);

		}

		[Fact]
		public async Task Exists_non_existing_blob_returns_false() {
			Assert.False(await _storage.ObjectExists(RandomBlobPath()));
		}

		[Fact]
		public async Task Exists_existing_blob_returns_true() {
			string id = RandomBlobPath();
			await _storage.SetText(id, "test");

			Assert.True(await _storage.ObjectExists(id));
		}

		[Fact]
		public async Task Delete_create_and_delete_doesnt_exist() {
			string path = RandomBlobPath();
			await _storage.SetText(path, "test");
			await _storage.DeleteObject(path);

			Assert.False(await _storage.ObjectExists(path));
		}

		[Fact]
		public async Task Delete_non_existing_file_ignores() {
			string path = RandomBlobPath();
			await _storage.DeleteObject(path);
		}

		[Fact]
		public async Task Delete_folder_removes_everything() {
			//setup
			string prefix = RandomBlobPath();
			string file1 = StoragePath.Combine(prefix, "1.txt");
			string file2 = StoragePath.Combine(prefix, "sub", "2.txt");


			try {
				//setup
				await _storage.SetText(file1, "1");
				await _storage.SetText(file2, "2");

				//act
				await _storage.DeleteObject(prefix);
			}
			catch (NotSupportedException) {

			}

			//assert
			List<StoreObject> files = await _storage.ListDirectory(prefix, true);
			Assert.True(files.Count == 0);
		}

		[Fact]
		public async Task Rename_File_Renames() {
			string prefix = RandomBlobPath();
			string file = StoragePath.Combine(prefix, "1");

			try {
				await _storage.SetText(file, "test");
				await _storage.MoveObject(file, StoragePath.Combine(prefix, "2"), true);
				List<StoreObject> list = await _storage.ListDirectory(prefix);

				Assert.Single(list);
				Assert.True(list.First().Name == "2");
			}
			catch (NotSupportedException) {

			}
		}

		[Fact]
		public async Task Rename_OldPathNull_ThowsArgumentNull() {
			await Assert.ThrowsAsync<ArgumentNullException>(() => _storage.MoveObject(null, "test/1", true));
		}

		[Fact]
		public async Task Rename_NewPathNull_ThowsArgumentNull() {
			await Assert.ThrowsAsync<ArgumentNullException>(() => _storage.MoveObject("test/1", null, true));
		}


		[Fact]
		public async Task Rename_Folder_Renames() {
			string prefix = RandomBlobPath();
			string file1 = StoragePath.Combine(prefix, "old", "1.txt");
			string file11 = StoragePath.Combine(prefix, "old", "1", "1.txt");
			string file111 = StoragePath.Combine(prefix, "old", "1", "1", "1.txt");

			try {
				await _storage.SetText(file1, string.Empty);
			}
			catch (NotSupportedException) {
				return;
			}

			await _storage.SetText(file11, string.Empty);
			await _storage.SetText(file111, string.Empty);

			await _storage.MoveObject(StoragePath.Combine(prefix, "old"), StoragePath.Combine(prefix, "new"), true);

			List<StoreObject> list = await _storage.ListDirectory(prefix);
		}

		[Fact]
		public async Task Read_larger_file() {
			string text = RandomGenerator.GetRandomString(1024 * 1024, false);

			try {
				await _storage.SetText("test/test", text);

				string text2 = await _storage.GetText("test/test");

				Assert.Equal(text, text2);
			}
			catch (NotSupportedException) {

			}
		}

		[Fact]
		public async Task UserMetadata_write_readsback() {
			var blob = new StoreObject(RandomBlobPath());
			blob.Metadata["user"] = "ivan";
			blob.Metadata["fun"] = "no";

			await _storage.SetText(blob, "test");
			StoreObject blob2 = await _storage.GetObjectInfo(blob);

			try {
				await _storage.SetObjectInfo(blob);
				blob2 = await _storage.GetObjectInfo(blob);
				Assert.True(blob2.Size > 0);
			}
			catch (NotSupportedException) {
				return;
			}

			//test
			blob2 = await _storage.GetObjectInfo(blob);
			Assert.NotNull(blob2.Metadata);
			Assert.Equal("ivan", blob2.Metadata["user"]);
			Assert.Equal("no", blob2.Metadata["fun"]);
			Assert.Equal(2, blob2.Metadata.Count);
		}

		[Fact]
		public async Task UserMetadata_OverwriteWithLess_RemovesOld() {
			//setup
			var blob = new StoreObject(RandomBlobPath());
			blob.Metadata["user"] = "ivan";
			blob.Metadata["fun"] = "no";
			await _storage.SetText(blob, "test");
			try {
				await _storage.SetObjectInfo(blob);
			}
			catch (NotSupportedException) {
				return;
			}
			blob.Metadata.Clear();
			blob.Metadata["user"] = "ivan2";
			await _storage.SetText(blob, "test2");
			await _storage.SetObjectInfo(blob);

			//test
			StoreObject blob2 = await _storage.GetObjectInfo(blob);
			Assert.NotNull(blob2.Metadata);
			Assert.Single(blob2.Metadata);
			Assert.Equal("ivan2", blob2.Metadata["user"]);
		}

		[Fact]
		public async Task UserMetadata_openwrite_readsback() {
			var blob = new StoreObject(RandomBlobPath());
			blob.Metadata["user"] = "ivan";
			blob.Metadata["fun"] = "no";

			await _storage.SetObject(blob, new MemoryStream(RandomGenerator.GetRandomBytes(10, 15)));

			try {
				await _storage.SetObjectInfo(blob);
			}
			catch (NotSupportedException) {
				return;
			}

			//test
			StoreObject blob2 = await _storage.GetObjectInfo(blob);
			Assert.NotNull(blob2.Metadata);
			Assert.Equal("ivan", blob2.Metadata["user"]);
			Assert.Equal("no", blob2.Metadata["fun"]);
			Assert.Equal(2, blob2.Metadata.Count);
		}

		[Fact]
		public async Task UserMetadata_List_AlsoReturnsMetadata() {
			var blob = new StoreObject(RandomBlobPath());
			blob.Metadata["user"] = "ivan";
			blob.Metadata["fun"] = "no";
			await _storage.SetText(blob, "test2");

			try {
				await _storage.SetObjectInfo(blob);
			}
			catch (NotSupportedException) {
				return;
			}

			List<StoreObject> all = await _storage.ListDirectory(folderPath: blob.FolderPath, includeAttributes: true);

			//test
			StoreObject blob2 = all.First(b => b.FullPath == blob.FullPath);
			Assert.NotNull(blob2.Metadata);
			Assert.Equal("ivan", blob2.Metadata["user"]);
			Assert.Equal("no", blob2.Metadata["fun"]);
			Assert.Equal(2, blob2.Metadata.Count);
		}

		[Fact]
		public async Task GetMd5HashAsync() {
			var blob = new StoreObject(RandomBlobPath());
			string content = RandomGenerator.RandomString;
			string hash = content.MD5();

			await _storage.SetText(blob, content);

			string hash2 = await _storage.GetObjectMD5(blob);
			Assert.Equal(hash, hash2);
		}

		[Fact]
		public async Task Hierarchy_CreateFolder_Exists() {
			string folderPath = RandomBlobPath();

			try {
				await _storage.CreateDirectory(folderPath, false);

				Assert.True(await _storage.DirectoryExists(folderPath));
			}
			catch (NotSupportedException) {

			}
		}

		private string RandomBlobPath(string prefix = null, string subfolder = null, string extension = "") {
			return StoragePath.Combine(
			   _blobPrefix,
			   subfolder,
			   (prefix ?? "") + Guid.NewGuid().ToString() + extension);
		}

		class TestDocument {
			public string M { get; set; }
		}

		// ---------------------------------------------------------------------
		// ListObjects
		// ---------------------------------------------------------------------

		[Fact]
		public async Task ListObjects_ReturnsUploadedObject() {
			string file = RandomFile();

			await CreateText(file);

			var list = await _storage.ListObjects();

			Assert.Contains(list, x => x.FullPath == file);
		}

		[Fact]
		public async Task ListObjects_ReturnsMultipleUploadedObjects() {
			string f1 = RandomFile();
			string f2 = RandomFile();
			string f3 = RandomFile();

			await CreateText(f1);
			await CreateText(f2);
			await CreateText(f3);

			var list = await _storage.ListObjects(new StorageListOptions { Recurse = true });

			Assert.Contains(list, x => x.FullPath == f1);
			Assert.Contains(list, x => x.FullPath == f2);
			Assert.Contains(list, x => x.FullPath == f3);
		}

		[Fact]
		public async Task ListObjects_AfterDelete_DoesNotContainDeletedObject() {
			string file = RandomFile();

			await CreateText(file);

			await _storage.DeleteObject(file);

			var list = await _storage.ListObjects();

			Assert.DoesNotContain(list, x => x.FullPath == file);
		}

		// ---------------------------------------------------------------------
		// ListDirectory(folder, recurse)
		// ---------------------------------------------------------------------

		[Fact]
		public async Task ListDirectory_EmptyFolder_ReturnsEmpty() {
			string folder = RandomFolder();

			var list = await _storage.ListDirectory(folder, false);

			Assert.NotNull(list);
			Assert.Empty(list);
		}

		[Fact]
		public async Task ListDirectory_ReturnsDirectChildren() {
			string folder = RandomFolder();

			await CreateText($"{folder}/a.txt");
			await CreateText($"{folder}/b.txt");

			var list = await _storage.ListDirectory(folder, false);

			Assert.Equal(2, list.Count);

			Assert.Contains(list, x => x.Name == "a.txt");
			Assert.Contains(list, x => x.Name == "b.txt");
		}

		[Fact]
		public async Task ListDirectory_NonRecursive_DoesNotReturnNestedFiles() {
			string folder = RandomFolder();

			await CreateText($"{folder}/root.txt");
			await CreateText($"{folder}/child/file.txt");

			var list = await _storage.ListDirectory(folder, false);

			Assert.Contains(list, x => x.Name == "root.txt");
			Assert.DoesNotContain(list, x => x.Name == "file.txt");
		}

		[Fact]
		public async Task ListDirectory_Recursive_ReturnsNestedFiles() {
			string folder = RandomFolder();

			await CreateText($"{folder}/root.txt");
			await CreateText($"{folder}/child/file.txt");
			await CreateText($"{folder}/child/sub/file2.txt");

			var list = (await _storage.ListDirectory(folder, true)).Where(f => f.Type == StorageObjectType.File).ToList();

			Assert.Equal(3, list.Count);

			Assert.Contains(list, x => x.Name == "root.txt");
			Assert.Contains(list, x => x.Name == "file.txt");
			Assert.Contains(list, x => x.Name == "file2.txt");
		}

		// ---------------------------------------------------------------------
		// ListDirectory(options)
		// ---------------------------------------------------------------------

		[Fact]
		public async Task ListDirectory_FilePrefix_ReturnsOnlyMatchingFiles() {
			string folder = RandomFolder();

			await CreateText($"{folder}/apple.txt");
			await CreateText($"{folder}/apricot.txt");
			await CreateText($"{folder}/banana.txt");

			var list = await _storage.ListDirectory(
				folderPath: folder,
				filePrefix: "ap",
				recurse: false);

			Assert.Equal(2, list.Count);

			Assert.All(list, x => Assert.StartsWith("ap", x.Name));
		}

		[Fact]
		public async Task ListDirectory_BrowseFilter_IsApplied() {
			string folder = RandomFolder();

			await CreateText($"{folder}/one.txt");
			await CreateText($"{folder}/two.txt");
			await CreateText($"{folder}/three.bin");

			var list = await _storage.ListDirectory(
				folderPath: folder,
				recurse: false,
				browseFilter: x => x.Name.EndsWith(".txt"));

			Assert.Equal(2, list.Count);

			Assert.All(list, x => Assert.EndsWith(".txt", x.Name));
		}

		[Fact]
		public async Task ListDirectory_MaxResults_IsRespected() {
			string folder = RandomFolder();

			for (int i = 0; i < 20; i++)
				await CreateText($"{folder}/{i}.txt");

			var list = await _storage.ListDirectory(
				folderPath: folder,
				recurse: true,
				maxResults: 5);

			Assert.True(list.Count <= 5);
		}

		[Fact]
		public async Task ListDirectory_FolderThatDoesNotExist_ReturnsEmpty() {
			var list = await _storage.ListDirectory(
				$"tests/{Guid.NewGuid():N}",
				true);

			Assert.NotNull(list);
			Assert.Empty(list);
		}

		// ---------------------------------------------------------------------
		// Metadata & Existence
		// ---------------------------------------------------------------------

		[Fact]
		public async Task ObjectExists_ExistingObject_ReturnsTrue() {
			string file = RandomFile();

			await CreateText(file);

			Assert.True(await _storage.ObjectExists(file));
		}

		[Fact]
		public async Task ObjectExists_MissingObject_ReturnsFalse() {
			Assert.False(await _storage.ObjectExists(RandomFile()));
		}

		[Fact]
		public async Task ObjectsExists_ReturnsStatusForEveryObject() {
			string f1 = RandomFile();
			string f2 = RandomFile();
			string f3 = RandomFile();

			await CreateText(f1);
			await CreateText(f3);

			var result = await _storage.ObjectsExists(new[]
			{
		f1,
		f2,
		f3
	});

			Assert.Equal(3, result.Count);

			Assert.True(result[0]);
			Assert.False(result[1]);
			Assert.True(result[2]);
		}

		[Fact]
		public async Task GetObjectInfo_ReturnsBasicInformation() {
			string file = RandomFile();

			await CreateText(file, "Hello World");

			var obj = await _storage.GetObjectInfo(file);

			Assert.NotNull(obj);

			Assert.Equal(file, obj.FullPath);
			Assert.True(obj.IsFile);
			Assert.False(obj.IsFolder);

			Assert.NotNull(obj.Size);
			Assert.True(obj.Size >= 11);
		}

		[Fact]
		public async Task GetObjectInfo_MissingObject_ReturnsNullOrThrows() {
			string file = RandomFile();

			try {
				var obj = await _storage.GetObjectInfo(file);

				Assert.Null(obj);
			}
			catch {
				// provider is allowed to throw
			}
		}

		[Fact]
		public async Task GetObjectsInfo_ReturnsInformationForMultipleObjects() {
			string f1 = RandomFile();
			string f2 = RandomFile();

			await CreateText(f1, "AAA");
			await CreateText(f2, "BBBB");

			var list = await _storage.GetObjectsInfo(new[]
			{
		f1,
		f2
	});

			Assert.Equal(2, list.Count);

			Assert.Contains(list, x => x.FullPath == f1);
			Assert.Contains(list, x => x.FullPath == f2);
		}

		[Fact]
		public async Task SetObjectInfo_DoesNotThrow() {
			string file = RandomFile();

			await CreateText(file);

			var obj = await _storage.GetObjectInfo(file);

			await _storage.SetObjectInfo(obj);

			var updated = await _storage.GetObjectInfo(file);

			Assert.NotNull(updated);
		}

		[Fact]
		public async Task SetObjectsInfo_DoesNotThrow() {
			string f1 = RandomFile();
			string f2 = RandomFile();

			await CreateText(f1);
			await CreateText(f2);

			var list = await _storage.GetObjectsInfo(new[]
			{
		f1,
		f2
	});

			await _storage.SetObjectsInfo(list);

			var verify = await _storage.GetObjectsInfo(new[]
			{
		f1,
		f2
	});

			Assert.Equal(2, verify.Count);
		}

		[Fact]
		public async Task GetObjectMD5_DoesNotThrow() {
			string file = RandomFile();

			await CreateText(file, "abcdef");

			var obj = await _storage.GetObjectInfo(file);

			string md5 = await _storage.GetObjectMD5(obj);

			// Some providers don't expose MD5.
			if (md5 != null)
				Assert.NotEmpty(md5);
		}

		[Fact]
		public async Task GetObjectLength_ReturnsCorrectLength() {
			string file = RandomFile();

			const string text = "Hello World";

			await CreateText(file, text);

			long length = await _storage.GetObjectLength(file);

			Assert.Equal(text.Length, length);
		}

		[Fact]
		public async Task GetObjectLength_MissingObject_ReturnsDefaultValue() {
			long length = await _storage.GetObjectLength(
				RandomFile(),
				defaultValue: 12345);

			Assert.Equal(12345, length);
		}

		[Fact]
		public async Task Metadata_AfterOverwrite_SizeChanges() {
			string file = RandomFile();

			await CreateText(file, "abc");

			long size1 = await _storage.GetObjectLength(file);

			await CreateText(file, "abcdefghijklmnopqrstuvwxyz");

			long size2 = await _storage.GetObjectLength(file);

			Assert.True(size2 > size1);
		}

		[Fact]
		public async Task Metadata_DateModified_IsNotBefore_DateCreated() {
			string file = RandomFile();

			await CreateText(file);

			var obj = await _storage.GetObjectInfo(file);

			if (obj.DateCreated.HasValue && obj.DateModified.HasValue) {
				Assert.True(obj.DateModified >= obj.DateCreated);
			}
		}

		[Fact]
		public async Task Metadata_FullPath_IsCorrect() {
			string file = RandomFile();

			await CreateText(file);

			var obj = await _storage.GetObjectInfo(file);

			Assert.Equal(file, obj.FullPath);
		}

		[Fact]
		public async Task Metadata_Name_IsCorrect() {
			string folder = RandomFolder();

			string file = $"{folder}/hello.txt";

			await CreateText(file);

			var obj = await _storage.GetObjectInfo(file);

			Assert.Equal("hello.txt", obj.Name);
		}

		[Fact]
		public async Task Metadata_FolderPath_IsCorrect() {
			string folder = RandomFolder();

			string file = $"{folder}/hello.txt";

			await CreateText(file);

			var obj = await _storage.GetObjectInfo(file);

			Assert.Equal(folder, obj.FolderPath);
		}

		// ---------------------------------------------------------------------
		// Read Operations
		// ---------------------------------------------------------------------

		[Fact]
		public async Task OpenRead_ReturnsReadableStream() {
			string file = RandomFile();

			await CreateText(file, "Hello World");

			using var stream = await _storage.OpenRead(file);

			Assert.NotNull(stream);
			Assert.True(stream.CanRead);
		}

		[Fact]
		public async Task OpenRead_ReadsEntireContents() {
			string file = RandomFile();

			const string text = "Hello World";

			await CreateText(file, text);

			using var stream = await _storage.OpenRead(file);
			using var reader = new StreamReader(stream);

			Assert.Equal(text, await reader.ReadToEndAsync());
		}

		[Fact]
		public async Task OpenRead_MissingObject_ReturnsNullOrThrows() {
			try {
				using var stream = await _storage.OpenRead(RandomFile());

				Assert.Null(stream);
			}
			catch {
				// acceptable
			}
		}

		[Fact]
		public async Task GetBytes_ReturnsCorrectBytes() {
			string file = RandomFile();

			byte[] expected = { 1, 2, 3, 4, 5, 6 };

			await _storage.SetBytes(file, expected);

			byte[] actual = await _storage.GetBytes(file);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public async Task GetBytes_EmptyFile_ReturnsEmptyArray() {
			string file = RandomFile();

			await _storage.SetBytes(file, Array.Empty<byte>());

			byte[] data = await _storage.GetBytes(file);

			Assert.NotNull(data);
			Assert.Empty(data);
		}

		[Fact]
		public async Task GetText_ReturnsCorrectText() {
			string file = RandomFile();

			const string text = "The quick brown fox.";

			await _storage.SetText(file, text);

			string actual = await _storage.GetText(file);

			Assert.Equal(text, actual);
		}

		[Fact]
		public async Task GetText_Unicode() {
			string file = RandomFile();

			const string text = "你好 नमस्ते 😀";

			await _storage.SetText(file, text);

			Assert.Equal(text, await _storage.GetText(file));
		}

		private sealed class TestPerson {
			public string Name { get; set; }
			public int Age { get; set; }
		}

		[Fact]
		public async Task GetJson_ReturnsObject() {
			string file = RandomFile();

			var person = new TestPerson {
				Name = "John",
				Age = 30
			};

			await _storage.SetJson(file, person);

			var loaded = await _storage.GetJson<TestPerson>(file);

			Assert.NotNull(loaded);
			Assert.Equal(person.Name, loaded.Name);
			Assert.Equal(person.Age, loaded.Age);
		}

		[Fact]
		public async Task GetJson_InvalidJson_Throws() {
			string file = RandomFile();

			await _storage.SetText(file, "this isn't json");

			await Assert.ThrowsAnyAsync<Exception>(
				() => _storage.GetJson<TestPerson>(file));
		}

		[Fact]
		public async Task GetJson_InvalidJson_Ignore_ReturnsNull() {
			string file = RandomFile();

			await _storage.SetText(file, "this isn't json");

			var result = await _storage.GetJson<TestPerson>(
				file,
				ignoreInvalidJson: true);

			Assert.Null(result);
		}

		[Fact]
		public async Task GetObject_CopiesToTargetStream() {
			string file = RandomFile();

			const string text = "Hello";

			await _storage.SetText(file, text);

			using var ms = new MemoryStream();

			await _storage.GetObject(file, ms);

			Assert.Equal(text,
				System.Text.Encoding.UTF8.GetString(ms.ToArray()));
		}

		[Fact]
		public async Task DownloadObject_DownloadsFile() {
			string file = RandomFile();

			const string text = "Downloaded";

			await _storage.SetText(file, text);

			string temp = Path.GetTempFileName();

			try {
				await _storage.DownloadObject(file, temp, overwrite: true);

				Assert.Equal(text, File.ReadAllText(temp));
			}
			finally {
				if (File.Exists(temp))
					File.Delete(temp);
			}
		}

		[Fact]
		public async Task OpenRange_ReturnsRequestedBytes() {
			string file = RandomFile();

			await _storage.SetText(file, "0123456789");

			using var stream = await _storage.OpenRange(file, 2, 4);

			using var reader = new StreamReader(stream);

			var result = await reader.ReadToEndAsync();

			if (result.Length == 4) {
				Assert.Equal("2345", result);
			}
			else {
				Assert.Equal("23456789", result);
			}
		}

		[Fact]
		public async Task OpenRange_FromBeginning() {
			string file = RandomFile();

			await _storage.SetText(file, "ABCDEFGHIJ");

			using var stream = await _storage.OpenRange(file, 0, 3);

			using var reader = new StreamReader(stream);

			var result = await reader.ReadToEndAsync();

			if (result.Length == 3) {
				Assert.Equal("ABC", result);
			}
			else {
				Assert.Equal("ABCDEFGHIJ", result);
			}
		}

		[Fact]
		public async Task OpenRange_BeyondEnd_ReturnsEmptyOrShortStream() {
			string file = RandomFile();

			await _storage.SetText(file, "abc");

			using var stream = await _storage.OpenRange(file, 100, 50);

			Assert.NotNull(stream);

			using var ms = new MemoryStream();

			await stream.CopyToAsync(ms);

			Assert.True(ms.Length == 0);
		}

		[Fact]
		public async Task OpenSeekable_ReturnsSeekableStream() {
			string file = RandomFile();

			await _storage.SetText(file, "abcdefghijklmnopqrstuvwxyz");

			using var stream = await _storage.OpenSeekable(file);

			Assert.NotNull(stream);
			Assert.True(stream.CanRead);
			Assert.True(stream.CanSeek);
		}

		[Fact]
		public async Task OpenSeekable_ReadSeekRead() {
			string file = RandomFile();

			await _storage.SetText(file, "0123456789");

			using var stream = await _storage.OpenSeekable(file);

			byte[] buffer = new byte[2];

			await stream.ReadAsync(buffer);

			Assert.Equal("01", System.Text.Encoding.UTF8.GetString(buffer));

			stream.Seek(5, SeekOrigin.Begin);

			await stream.ReadAsync(buffer);

			Assert.Equal("56", System.Text.Encoding.UTF8.GetString(buffer));
		}

		[Fact]
		public async Task OpenSeekable_LengthMatchesObject() {
			string file = RandomFile();

			const string text = "Hello World";

			await _storage.SetText(file, text);

			using var stream = await _storage.OpenSeekable(file);

			Assert.Equal(text.Length, stream.Length);
		}

		[Fact]
		public async Task OpenSeekable_MissingObject_ReturnsNullOrThrows() {
			try {
				using var stream = await _storage.OpenSeekable(RandomFile());

				Assert.Null(stream);
			}
			catch {
				// acceptable
			}
		}

		// ---------------------------------------------------------------------
		// Write Operations
		// ---------------------------------------------------------------------

		[Fact]
		public async Task SetText_CreatesObject() {
			string file = RandomFile();

			await _storage.SetText(file, "Hello");

			Assert.True(await _storage.ObjectExists(file));
		}

		[Fact]
		public async Task SetText_OverwritesExistingContents() {
			string file = RandomFile();

			await _storage.SetText(file, "One");
			await _storage.SetText(file, "Two");

			Assert.Equal("Two", await _storage.GetText(file));
		}

		[Fact]
		public async Task SetBytes_WritesBytes() {
			string file = RandomFile();

			byte[] expected = { 10, 20, 30, 40 };

			await _storage.SetBytes(file, expected);

			byte[] actual = await _storage.GetBytes(file);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public async Task SetBytes_EmptyArray_CreatesEmptyFile() {
			string file = RandomFile();

			await _storage.SetBytes(file, Array.Empty<byte>());

			Assert.True(await _storage.ObjectExists(file));
			Assert.Equal(0, await _storage.GetObjectLength(file));
		}

		[Fact]
		public async Task SetJson_WritesObject() {
			string file = RandomFile();

			var person = new TestPerson {
				Name = "Alice",
				Age = 25
			};

			await _storage.SetJson(file, person);

			var loaded = await _storage.GetJson<TestPerson>(file);

			Assert.Equal(person.Name, loaded.Name);
			Assert.Equal(person.Age, loaded.Age);
		}

		[Fact]
		public async Task SetObject_Stream_WritesData() {
			string file = RandomFile();

			byte[] bytes = System.Text.Encoding.UTF8.GetBytes("Hello World");

			using var ms = new MemoryStream(bytes);

			await _storage.SetObject(file, ms);

			Assert.Equal("Hello World", await _storage.GetText(file));
		}

		[Fact]
		public async Task SetObject_WithContentType_WritesData() {
			string file = RandomFile();

			byte[] bytes = System.Text.Encoding.UTF8.GetBytes("abcdef");

			using var ms = new MemoryStream(bytes);

			await _storage.SetObject(file, ms, "text/plain");

			Assert.Equal("abcdef", await _storage.GetText(file));
		}

		[Fact]
		public async Task SetObject_Append_AppendsData() {
			string file = RandomFile();

			using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Hello")))
				await _storage.SetObject(file, ms);

			using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(" World")))
				await _storage.SetObject(file, ms, append: true);

			Assert.Equal("Hello World", await _storage.GetText(file));
		}

		[Fact]
		public async Task OpenWrite_CreatesObject() {
			string file = RandomFile();

			using (var stream = await _storage.OpenWrite(file, overwrite: true))
			using (var writer = new StreamWriter(stream)) {
				await writer.WriteAsync("Hello");
			}

			Assert.Equal("Hello", await _storage.GetText(file));
		}

		[Fact]
		public async Task OpenWrite_Overwrite_ReplacesExistingContents() {
			string file = RandomFile();

			await _storage.SetText(file, "Old");

			using (var stream = await _storage.OpenWrite(file, overwrite: true))
			using (var writer = new StreamWriter(stream)) {
				await writer.WriteAsync("New");
			}

			Assert.Equal("New", await _storage.GetText(file));
		}

		[Fact]
		public async Task OpenWrite_OverwriteFalse_ReturnsNullOrThrows_WhenObjectExists() {
			string file = RandomFile();

			await _storage.SetText(file, "Existing");

			try {
				using var stream = await _storage.OpenWrite(file, overwrite: false);

				Assert.Null(stream);
			}
			catch {
				// Provider may throw instead.
			}
		}

		[Fact]
		public async Task UploadObject_UploadsLocalFile() {
			string file = RandomFile();

			string temp = Path.GetTempFileName();

			try {
				File.WriteAllText(temp, "Upload Test");

				await _storage.UploadObject(file, temp, overwrite: true);

				Assert.Equal("Upload Test", await _storage.GetText(file));
			}
			finally {
				if (File.Exists(temp))
					File.Delete(temp);
			}
		}

		[Fact]
		public async Task UploadObject_Overwrite_ReplacesExistingObject() {
			string file = RandomFile();

			await _storage.SetText(file, "Old");

			string temp = Path.GetTempFileName();

			try {
				File.WriteAllText(temp, "New");

				await _storage.UploadObject(file, temp, overwrite: true);

				Assert.Equal("New", await _storage.GetText(file));
			}
			finally {
				if (File.Exists(temp))
					File.Delete(temp);
			}
		}

		[Fact]
		public async Task UploadObject_OverwriteFalse_ReturnsFalseOrThrows_WhenObjectExists() {
			string file = RandomFile();

			await _storage.SetText(file, "Existing");

			string temp = Path.GetTempFileName();

			try {
				File.WriteAllText(temp, "Replacement");

				try {
					await _storage.UploadObject(file, temp, overwrite: false);

					Assert.Equal("Existing", await _storage.GetText(file));
				}
				catch {
					// acceptable
				}
			}
			finally {
				if (File.Exists(temp))
					File.Delete(temp);
			}
		}

		[Fact]
		public async Task LargeText_CanBeWrittenAndRead() {
			string file = RandomFile();

			string text = new string('X', 1024 * 1024);

			await _storage.SetText(file, text);

			Assert.Equal(text, await _storage.GetText(file));
		}

		[Fact]
		public async Task UnicodeFileName_CanBeWritten() {
			string file = $"tests/{Guid.NewGuid():N}/你好 नमस्ते 😀.txt";

			await _storage.SetText(file, "unicode");

			Assert.True(await _storage.ObjectExists(file));

			Assert.Equal("unicode", await _storage.GetText(file));
		}

		[Fact]
		public async Task ConsecutiveWrites_LastWriteWins() {
			string file = RandomFile();

			for (int i = 0; i < 10; i++)
				await _storage.SetText(file, i.ToString());

			Assert.Equal("9", await _storage.GetText(file));
		}

		[Fact]
		public async Task BinaryFile_RoundTripsCorrectly() {
			string file = RandomFile();

			byte[] expected = new byte[8192];

			new Random(12345).NextBytes(expected);

			await _storage.SetBytes(file, expected);

			byte[] actual = await _storage.GetBytes(file);

			Assert.Equal(expected, actual);
		}

	}
}
