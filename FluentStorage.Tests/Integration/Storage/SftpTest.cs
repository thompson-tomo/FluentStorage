using Renci.SshNet;

namespace FluentStorage.Tests.Integration.Storage {
	public class SftpTestFixture : IStoreFixture {
		protected override IStore CreateStorage(ITestConfig settings) {

			/// By default the SSH private key of the admin PC is stored in `C:\Users\[USER]\.ssh\id_ed[`.
			string privateKey = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".ssh\id_ed25519");

			var keyFile = new PrivateKeyFile(privateKey, settings.SftpPassphrase);
			var connInfo = new PrivateKeyConnectionInfo(settings.SftpHost, settings.SftpPort, settings.SftpUser, keyFile);

			return SftpStorage.FromConnectionInfo(connInfo);
		}
	}

	public class SftpTest : IStoreTest, IClassFixture<SftpTestFixture> {
		public SftpTest(SftpTestFixture fixture) : base(fixture) {
		}
	}
}
