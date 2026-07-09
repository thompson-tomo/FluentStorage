using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentStorage.Storage;
using Xunit;

namespace FluentStorage.Tests.Blobs {
	public class VirtualStorageTest {
		private readonly IVirtualStorage _vs = StorageFactory.Virtual();
		private readonly IBucket _ms0 = StorageFactory.InMemory();
		private readonly IBucket _ms1 = StorageFactory.InMemory();
		private readonly IBucket _ms2 = StorageFactory.InMemory();

		public VirtualStorageTest() {
			_vs.Mount("/", _ms0);
			_vs.Mount("/mnt/ms1", _ms1);
			_vs.Mount("/mnt/ms2", _ms2);
		}

		[Fact]
		public async Task Return_files_and_mounts_one_mount() {
			IReadOnlyCollection<StorageObject> all = await _vs.ListAsync();

			Assert.Equal(1, all.Count);   // "mnt" folder
			Assert.Equal(new StorageObject("/mnt", BlobItemKind.Folder), all.First());
		}

		[Fact]
		public async Task Return_files_and_mounts_one_mount_and_one_file() {
			await _ms0.WriteTextAsync("1.txt", "test");

			IReadOnlyCollection<StorageObject> all = await _vs.ListAsync();

			Assert.Equal(2, all.Count);   // "mnt" folder
			Assert.Equal(new StorageObject("/mnt", BlobItemKind.Folder), all.First());
			Assert.Equal(new StorageObject("1.txt"), all.Skip(1).First());
		}

		[Fact]
		public async Task Mass_exists_calls_both_mounts() {
			await _ms1.WriteTextAsync("ms1.txt", "dfadf");
			await _ms2.WriteTextAsync("ms2.txt", "dfafsdf");

			await _vs.ExistsAsync(new[] { "/mnt/ms1/ms1.txt", "/mnt/ms2/ms2.txt" });
		}
	}
}
