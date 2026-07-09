using FluentStorage.Storage;

using System.IO;

using Xunit;

namespace FluentStorage.Tests.Integration.Blobs {
	public class AzureBlobStorageFixture : BlobFixture {
		public AzureBlobStorageFixture() : base("lakeyv12") {
		}

		protected override IBucket CreateStorage(ITestSettings settings) {
			return AzureBlobStorage.FromSharedKey(settings.AzureStorageName, settings.AzureStorageKey);
			//.WithGzipCompression();
		}
	}

	public class AzureBlobStorageTest : BlobTest, IClassFixture<AzureBlobStorageFixture> {
		public AzureBlobStorageTest(AzureBlobStorageFixture fixture) : base(fixture) {
		}
	}

#if DEBUG
	public class AzureEmulatedBlobStorageFixture : BlobFixture {
		public AzureEmulatedBlobStorageFixture() : base("itest") {

		}

		protected override IBucket CreateStorage(ITestSettings settings) {
			return AzureBlobStorage.FromLocalEmulator();
		}
	}

	public class AzureEmulatedBlobStorageTest : BlobTest, IClassFixture<AzureEmulatedBlobStorageFixture> {
		public AzureEmulatedBlobStorageTest(AzureEmulatedBlobStorageFixture fixture) : base(fixture) {

		}
	}
#endif

	public class AzureFilesFixture : BlobFixture {
		public AzureFilesFixture() : base("testshare") {

		}

		protected override IBucket CreateStorage(ITestSettings settings) {
			return AzureFilesStorage.FromCredentials(settings.AzureStorageName, settings.AzureStorageKey);
		}
	}

	public class AzureFilesTest : BlobTest, IClassFixture<AzureFilesFixture> {
		public AzureFilesTest(AzureFilesFixture fixture) : base(fixture) {

		}
	}

	public class AdlsGen2Fixture : BlobFixture {
		public AdlsGen2Fixture() : base("integration") {

		}

		protected override IBucket CreateStorage(ITestSettings settings) {
			return AzureDataLakeStorage.FromSharedKey(
			   settings.AzureGen2StorageName,
			   settings.AzureGen2StorageKey);

			//return StorageFactory.AzureDataLakeGen2StoreBySharedAccessKey(settings.AzureDataLakeGen2Name, settings.AzureDataLakeGen2Key);
		}
	}

	public class AdlsGen2Test : BlobTest, IClassFixture<AdlsGen2Fixture> {
		public AdlsGen2Test(AdlsGen2Fixture fixture) : base(fixture) {
		}
	}

	public class DiskDirectoryStorageFixture : BlobFixture {
		protected override IBucket CreateStorage(ITestSettings settings) {
			return StorageFactory.DirectoryFiles(TestDir);
		}
	}

	public class DiskDirectoryTest : BlobTest, IClassFixture<DiskDirectoryStorageFixture> {
		public DiskDirectoryTest(DiskDirectoryStorageFixture fixture) : base(fixture) {
		}
	}

	public class ZipFileFixture : BlobFixture {
		protected override IBucket CreateStorage(ITestSettings settings) {
			return StorageFactory.ZipFile(Path.Combine(TestDir, "test.zip"));
		}
	}

	public class ZipFileTest : BlobTest, IClassFixture<ZipFileFixture> {
		public ZipFileTest(ZipFileFixture fixture) : base(fixture) {
		}
	}

	public class AwsS3Fixture : BlobFixture {
		protected override IBucket CreateStorage(ITestSettings settings) {
			return AwsS3Storage.FromCredentials(
					 settings.AwsAccessKeyId,
					 settings.AwsSecretAccessKey,
					 null,
					 settings.AwsTestBucketName,
					 settings.AwsTestBucketRegion);
		}
	}

	public class AwsS3Test : BlobTest, IClassFixture<AwsS3Fixture> {
		public AwsS3Test(AwsS3Fixture fixture) : base(fixture) {
		}
	}

	public class InMemoryFixture : BlobFixture {
		protected override IBucket CreateStorage(ITestSettings settings) {
			return StorageFactory.InMemory();
		}
	}

	public class InMemoryTest : BlobTest, IClassFixture<InMemoryFixture> {
		public InMemoryTest(InMemoryFixture fixture) : base(fixture) {
		}
	}

	public class AzureKeyVaultFixture : BlobFixture {
		protected override IBucket CreateStorage(ITestSettings settings) {
			return AzureKeyVaultStorage.FromCredentials(
					 settings.AzureKeyVaultUri,
					 settings.TenantId,
					 settings.ClientId,
					 settings.ClientSecret);
		}
	}

	public class AzureKeyVaultTest : BlobTest, IClassFixture<AzureKeyVaultFixture> {
		public AzureKeyVaultTest(AzureKeyVaultFixture fixture) : base(fixture) {
		}
	}

	public class GcpFixture : BlobFixture {
		protected override IBucket CreateStorage(ITestSettings settings) {
			return GoogleCloudStorage.FromJson(
			   settings.GcpStorageBucketName,
			   settings.GcpStorageJsonCreds,
			   true);
		}
	}

	public class GcpTest : BlobTest, IClassFixture<GcpFixture> {
		public GcpTest(GcpFixture fixture) : base(fixture) {

		}
	}

	public class VirtualStorageFixture : BlobFixture {
		protected override IBucket CreateStorage(ITestSettings settings) {
			IVirtualStorage vs = StorageFactory.Virtual();
			vs.Mount("/", StorageFactory.InMemory());
			vs.Mount("/mnt/s0", StorageFactory.InMemory());
			return vs;
		}
	}

	public class VirtualStorageTest : BlobTest, IClassFixture<VirtualStorageFixture> {
		public VirtualStorageTest(VirtualStorageFixture fixture) : base(fixture) {

		}
	}
}
