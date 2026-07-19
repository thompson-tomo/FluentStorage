using FluentStorage.Tests.Integration.Config;
using FluentStorage.Tests.Unit.Core;

namespace FluentStorage.Tests.Integration.Storage {
	public class AzureBlobFixture : IStoreFixture {
		public AzureBlobFixture() : base("lakeyv12") {
		}

		protected override IStore CreateStorage(ITestConfig settings) {
			return AzureBlobStorage.FromSharedKey(settings.AzureStorageName, settings.AzureStorageKey);
			//.WithGzipCompression();
		}
	}

	public class AzureBlobStorageTest : IStoreTest, IClassFixture<AzureBlobFixture> {
		public AzureBlobStorageTest(AzureBlobFixture fixture) : base(fixture) {
		}
	}

#if DEBUG
	public class AzureEmulatedBlobFixture : IStoreFixture {
		public AzureEmulatedBlobFixture() : base("itest") {

		}

		protected override IStore CreateStorage(ITestConfig settings) {
			return AzureBlobStorage.FromLocalEmulator();
		}
	}

	public class AzureEmulatedBlobStorageTest : IStoreTest, IClassFixture<AzureEmulatedBlobFixture> {
		public AzureEmulatedBlobStorageTest(AzureEmulatedBlobFixture fixture) : base(fixture) {

		}
	}
#endif

	public class AzureFilesFixture : IStoreFixture {
		public AzureFilesFixture() : base("testshare") {

		}

		protected override IStore CreateStorage(ITestConfig settings) {
			return AzureFilesStorage.FromCredentials(settings.AzureStorageName, settings.AzureStorageKey);
		}
	}

	public class AzureFilesTest : IStoreTest, IClassFixture<AzureFilesFixture> {
		public AzureFilesTest(AzureFilesFixture fixture) : base(fixture) {

		}
	}

	public class AdlsGen2Fixture : IStoreFixture {
		public AdlsGen2Fixture() : base("integration") {

		}

		protected override IStore CreateStorage(ITestConfig settings) {
			return AzureDataLakeStorage.FromSharedKey(
			   settings.AzureGen2StorageName,
			   settings.AzureGen2StorageKey);

			//return StorageFactory.AzureDataLakeGen2StoreBySharedAccessKey(settings.AzureDataLakeGen2Name, settings.AzureDataLakeGen2Key);
		}
	}

	public class AdlsGen2Test : IStoreTest, IClassFixture<AdlsGen2Fixture> {
		public AdlsGen2Test(AdlsGen2Fixture fixture) : base(fixture) {
		}
	}

	public class LocalDiskTestFixture : IStoreFixture {
		protected override IStore CreateStorage(ITestConfig settings) {
			return StorageFactory.Disk(TestDir);
		}
	}

	public class LocalDiskTest : IStoreTest, IClassFixture<LocalDiskTestFixture> {
		public LocalDiskTest(LocalDiskTestFixture fixture) : base(fixture) {
		}
	}

	public class AwsS3Fixture : IStoreFixture {
		protected override IStore CreateStorage(ITestConfig settings) {
			return AwsS3Storage.FromCredentials(
					 settings.AwsAccessKeyId,
					 settings.AwsSecretAccessKey,
					 null,
					 settings.AwsTestBucketName,
					 settings.AwsTestBucketRegion);
		}
	}

	public class AwsS3Test : IStoreTest, IClassFixture<AwsS3Fixture> {
		public AwsS3Test(AwsS3Fixture fixture) : base(fixture) {
		}
	}

	public class InMemoryFixture : IStoreFixture {
		protected override IStore CreateStorage(ITestConfig settings) {
			return StorageFactory.InMemory();
		}
	}

	public class InMemoryTest : IStoreTest, IClassFixture<InMemoryFixture> {
		public InMemoryTest(InMemoryFixture fixture) : base(fixture) {
		}
	}

	public class AzureKeyVaultFixture : IStoreFixture {
		protected override IStore CreateStorage(ITestConfig settings) {
			return AzureKeyVaultStorage.FromCredentials(
					 settings.AzureKeyVaultUri,
					 settings.TenantId,
					 settings.ClientId,
					 settings.ClientSecret);
		}
	}

	public class AzureKeyVaultTest : IStoreTest, IClassFixture<AzureKeyVaultFixture> {
		public AzureKeyVaultTest(AzureKeyVaultFixture fixture) : base(fixture) {
		}
	}

	public class GcpFixture : IStoreFixture {
		protected override IStore CreateStorage(ITestConfig settings) {
			return GoogleCloudStorage.FromJson(
			   settings.GcpStorageBucketName,
			   settings.GcpStorageJsonCreds,
			   true);
		}
	}

	public class GcpTest : IStoreTest, IClassFixture<GcpFixture> {
		public GcpTest(GcpFixture fixture) : base(fixture) {

		}
	}

}
