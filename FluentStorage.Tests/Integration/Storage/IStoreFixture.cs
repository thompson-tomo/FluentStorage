using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Config.Net;
using FluentStorage.Model;
using FluentStorage.Storage;
using FluentStorage.Tests.Integration.Config;

namespace FluentStorage.Tests.Integration.Storage {
	public abstract class IStoreFixture : IDisposable {
		private static readonly ITestConfig _settings = TestConfigLoader.Instance;

		private string _testDir;
		private bool _initialised;

		protected IStoreFixture(string blobPrefix = null) {
			Storage = CreateStorage(_settings);
			BlobPrefix = blobPrefix;
		}

		protected abstract IStore CreateStorage(ITestConfig settings);

		public IStore Storage { get; private set; }
		public string BlobPrefix { get; }

		public string TestDir {
			get {
				if (_testDir == null) {
					string buildDir = @"C:\Temp\FluentStorage\";
					_testDir = Path.Combine(buildDir, "TEST -" + Guid.NewGuid());
				}

				return _testDir;
			}
		}

		public async Task InitAsync() {
			if (_initialised)
				return;

			//drop all blobs in test storage

			List<StoreObject> topLevel = (await Storage.ListDirectory(BlobPrefix, false)).ToList();

			try {
				await Storage.DeleteObjects(topLevel.Select(f => f.FullPath));
			}
			catch {
				//absolutely doesn't matter if it fails, this is only a perf improvement on tests
			}

			_initialised = true;
		}

		public Task DisposeAsync() {
			return Task.CompletedTask;
		}

		public void Dispose() {
			Storage.Dispose();

			if (_testDir != null) {
				try {
					Directory.Delete(_testDir, true);
				}
				catch (Exception) {}
				_testDir = null;
			}
		}
	}
}
