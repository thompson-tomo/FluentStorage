using FluentStorage.Queue;
using System;
using Config.Net;
using System.IO;
using System.Reflection;
using FluentStorage.Tests.Integration.Config;

namespace FluentStorage.Tests.Integration.Queue {
	public abstract class MessagingFixture : IDisposable {
		private static readonly ITestConfig _settings = TestConfigLoader.Config;
		public readonly IQueue Messenger;
		private readonly string _fixtureName;
		protected readonly string _testDir;

		protected MessagingFixture() {
			_fixtureName = GetType().Name;
			string buildDir = new FileInfo(new Uri(Assembly.GetExecutingAssembly().Location).LocalPath).Directory.FullName;
			_testDir = Path.Combine(buildDir, "msg-" + Guid.NewGuid());
			Directory.CreateDirectory(_testDir);

			Messenger = CreateMessenger(_settings);
		}

		protected abstract IQueue CreateMessenger(ITestConfig settings);

		public void Dispose() {
			if (Messenger != null)
				Messenger.Dispose();

			Directory.Delete(_testDir, true);
		}
	}
}