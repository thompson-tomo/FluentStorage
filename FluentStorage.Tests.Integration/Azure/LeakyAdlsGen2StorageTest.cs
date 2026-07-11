using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentStorage.Storage;
using Xunit;
using FluentStorage.Azure.Blobs.DataLake;
using FluentStorage.Azure.Blobs.DataLake.Model;
using FluentStorage.Azure.Blobs;

namespace FluentStorage.Tests.Integration.Azure {
	[Trait("Category", "Blobs")]
	public class LeakyAdlsGen2StorageTest : IAsyncLifetime {
		private readonly ITestSettings _settings;
		private readonly IAzureDataLakeStorage _storage;
		private static readonly string Filesystem = nameof(LeakyAdlsGen2StorageTest).ToLower();

		public LeakyAdlsGen2StorageTest() {
			_settings = Settings.Instance;
			_storage = AzureDataLakeStorage.FromAzureAd(
			   _settings.AzureGen2StorageName,
			   _settings.TenantId,
			   _settings.ClientId,
			   _settings.ClientSecret);
		}

		[Fact]
		public async Task Authenticate_with_shared_key() {
			IAzureDataLakeStorage authInstance =
			   AzureDataLakeStorage.FromSharedKey(_settings.AzureGen2StorageName,
				  _settings.AzureGen2StorageKey);

			//trigger any operation
			await authInstance.ListObjects();
		}

		[Fact]
		public async Task Authenticate_with_service_principal() {
			//needs to have "Storage Blob Data Owner"

			IBucket authInstance = AzureDataLakeStorage.FromAzureAd(
			   _settings.AzureGen2StorageName,
			   _settings.TenantId,
			   _settings.ClientId,
			   _settings.ClientSecret);

			//trigger any operation
			await authInstance.ListObjects();
		}

		[Fact]
		public async Task FS_list_doesnt_crash() {
			List<Filesystem> list = await _storage.ListFilesystemsAsync();

			Assert.True(list.Count > 0);
		}

		[Fact]
		public async Task FS_Creates_deletes_and_lists() {
			string filesystem = "createfs-" + Guid.NewGuid().ToString();

			try {
				//await _storage.DeleteFilesystemAsync(filesystem);
				Assert.DoesNotContain(await _storage.ListFilesystemsAsync(), x => x.Name == filesystem);

				await _storage.CreateFilesystemAsync(filesystem);
				Assert.Contains(await _storage.ListFilesystemsAsync(), x => x.Name == filesystem);
			}
			finally {
				await _storage.DeleteFilesystemAsync(filesystem);
			}
			Assert.DoesNotContain(await _storage.ListFilesystemsAsync(), x => x.Name == filesystem);
		}

		[Fact]
		public async Task FS_GetProperties() {
			string fsName = "propfs";

			await _storage.SetText(fsName + "/fff", "test");

			StorageObject fsBlob = await _storage.GetObjectInfo(fsName);


		}

		[Fact]
		public async Task Acl_assign_permisssions_to_file_for_user() {
			string path = StoragePath.Combine(Filesystem, Guid.NewGuid().ToString());
			string userId = _settings.OperatorObjectId;

			//write something
			await _storage.SetText(path, "perm?");

			//check that user has no permissions
			AccessControl access = await _storage.GetAccessControlAsync(path);
			Assert.DoesNotContain(access.Acl, x => x.Identity == userId);

			//assign user a write permission
			access.Acl.Add(new AclEntry(ObjectType.User, userId, false, true, false));
			await _storage.SetAccessControlAsync(path, access);

			//check user has permissions now
			access = await _storage.GetAccessControlAsync(path);
			AclEntry userAcl = access.Acl.First(e => e.Identity == userId);
			Assert.False(userAcl.CanRead);
			Assert.True(userAcl.CanWrite);
			Assert.False(userAcl.CanExecute);
		}

		[Fact]
		public async Task Acl_get_with_upn() {
			string path = StoragePath.Combine(Filesystem, Guid.NewGuid().ToString());
			string userId = _settings.OperatorObjectId;

			//write something
			await _storage.SetText(path, "perm?");

			//check that user has no permissions
			AccessControl access = await _storage.GetAccessControlAsync(path, true);
			Assert.DoesNotContain(access.Acl, x => x.Identity == userId);

			//assign user a write permission
			access.Acl.Add(new AclEntry(ObjectType.User, userId, false, true, false));
			await _storage.SetAccessControlAsync(path, access);

			//check user has permissions now
			access = await _storage.GetAccessControlAsync(path, true);
			Assert.True(access.Acl.First().Identity.Contains('@'));
		}

		[Fact]
		public async Task Acl_assign_non_default_permisssions_to_directory_for_user() {
			string directoryPath = StoragePath.Combine(Filesystem, "aclnondefault");
			string filePath = StoragePath.Combine(directoryPath, Guid.NewGuid().ToString());
			string userId = _settings.OperatorObjectId;

			//write something
			await _storage.SetText(filePath, "perm?");

			//check that user has no permissions
			AccessControl access = await _storage.GetAccessControlAsync(directoryPath);
			Assert.DoesNotContain(access.Acl, x => x.Identity == userId);

			//assign user a write permission
			access.Acl.Add(new AclEntry(ObjectType.User, userId, false, true, false));
			await _storage.SetAccessControlAsync(directoryPath, access);

			//check user has permissions now
			access = await _storage.GetAccessControlAsync(directoryPath);
			AclEntry userAcl = access.Acl.First(e => e.Identity == userId);
			Assert.False(userAcl.CanRead);
			Assert.True(userAcl.CanWrite);
			Assert.False(userAcl.CanExecute);
		}

		[Fact]
		public async Task Acl_assign_default_permisssions_to_directory_for_user() {
			string directoryPath = StoragePath.Combine(Filesystem, "acldefault");
			string filePath = StoragePath.Combine(directoryPath, Guid.NewGuid().ToString());
			string userId = _settings.OperatorObjectId;

			//write something
			await _storage.SetText(filePath, "perm?");

			//check that user has no permissions
			AccessControl access = await _storage.GetAccessControlAsync(directoryPath);
			Assert.DoesNotContain(access.Acl, x => x.Identity == userId);

			//assign user a write permission
			access.DefaultAcl.Add(new AclEntry(ObjectType.User, userId, false, true, false));
			await _storage.SetAccessControlAsync(directoryPath, access);

			//check user has permissions now
			access = await _storage.GetAccessControlAsync(directoryPath);
			AclEntry userAcl = access.DefaultAcl.First(e => e.Identity == userId);
			Assert.False(userAcl.CanRead);
			Assert.True(userAcl.CanWrite);
			Assert.False(userAcl.CanExecute);
		}

		[Fact]
		public async Task Acl_assign_non_default_permisssions_to_filesystem_for_user() {
			string filesystem = "aclnondefault";
			string userId = _settings.OperatorObjectId;

			//create filesystem
			await _storage.CreateFilesystemAsync(filesystem);

			//check that user has no permissions
			AccessControl access = await _storage.GetAccessControlAsync(filesystem);
			Assert.DoesNotContain(access.Acl, x => x.Identity == userId);

			//assign user a write permission
			access.Acl.Add(new AclEntry(ObjectType.User, userId, false, true, false));
			await _storage.SetAccessControlAsync(filesystem, access);

			//check user has permissions now
			access = await _storage.GetAccessControlAsync(filesystem);

			//delete filesystem
			await _storage.DeleteFilesystemAsync(filesystem);

			AclEntry userAcl = access.Acl.First(e => e.Identity == userId);
			Assert.False(userAcl.CanRead);
			Assert.True(userAcl.CanWrite);
			Assert.False(userAcl.CanExecute);
		}

		[Fact]
		public async Task Acl_assign_default_permisssions_to_filesystem_for_user() {
			string filesystem = "acldefault";
			string userId = _settings.OperatorObjectId;

			//create filesystem
			await _storage.CreateFilesystemAsync(filesystem);

			//check that user has no permissions
			AccessControl access = await _storage.GetAccessControlAsync(filesystem);
			Assert.DoesNotContain(access.Acl, x => x.Identity == userId);

			//assign user a write permission
			access.DefaultAcl.Add(new AclEntry(ObjectType.User, userId, false, true, false));
			await _storage.SetAccessControlAsync(filesystem, access);

			//check user has permissions now
			access = await _storage.GetAccessControlAsync(filesystem);

			//delete filesystem
			await _storage.DeleteFilesystemAsync(filesystem);

			AclEntry userAcl = access.DefaultAcl.First(e => e.Identity == userId);
			Assert.False(userAcl.CanRead);
			Assert.True(userAcl.CanWrite);
			Assert.False(userAcl.CanExecute);
		}

		public async Task InitializeAsync() {
			//drop all blobs in test storage
			List<StorageObject> topLevel =
			   (await _storage.ListDirectory(recurse: false, folderPath: Filesystem)).ToList();

			try {
				await _storage.DeleteObject(topLevel.Select(f => f.FullPath));
			}
			catch {
				//suppress exception to resume test attempt
			}
		}

		public Task DisposeAsync() {
			return Task.CompletedTask;
		}
	}
}
