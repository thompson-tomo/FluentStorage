namespace FluentStorage.Tests.Integration.Storage {
	public class AzureKeyVaultFixture : IStoreFixture {
		protected override IStore CreateStorage(ITestConfig settings) {
			return AzureKeyVaultStorage.FromCredentials(
					 settings.AzureKeyVaultUri,
					 settings.AzureTenantId,
					 settings.AzureClientId,
					 settings.AzureClientSecret);
		}
	}

	public class AzureKeyVaultTest : IStoreTest, IClassFixture<AzureKeyVaultFixture> {
		public AzureKeyVaultTest(AzureKeyVaultFixture fixture) : base(fixture) {
		}
	}
}
