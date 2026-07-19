namespace FluentStorage.Tests.Integration.Storage {
	public class FtpTestFixture : IStoreFixture {
		protected override IStore CreateStorage(ITestConfig settings) {
			return FtpStorage.FromCredentials(settings.FtpHost,
				new System.Net.NetworkCredential(settings.FtpUsername, settings.FtpPassword),
				FluentFTP.FtpDataConnectionType.AutoActive);
		}
	}

	public class FtpTest : IStoreTest, IClassFixture<FtpTestFixture> {
		public FtpTest(FtpTestFixture fixture) : base(fixture) {
		}
	}
}
