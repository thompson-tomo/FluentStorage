## Azure Data Lake Gen 1 [deprecated]

<img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/azure-data-lake.png" width="128" align="right"></img> 
In order to use Azure DataLake Gen 1, reference [![NuGet](https://img.shields.io/nuget/v/FluentStorage.Azure.DataLake.svg)](https://www.nuget.org/packages/FluentStorage.Azure.DataLake/) package first.

To create using a factory method, use the following signature:

```csharp
IBlobStorage storage = StorageFactory.Blobs.AzureDataLakeGen1StoreByClientSecret(
         string accountName,
         string tenantId,
         string principalId,
         string principalSecret,
         int listBatchSize = 5000)
```

The last parameter *listBatchSize* indicates how to query storage for list operations - by default a batch of 5k items will be used. Note that the larger the batch size, the more data you will receive in the request. This speeds up list operations, however may result in HTTP time-out the slower your internet connection is. This feature is not available in the standard .NET SDK and was implemented from scratch.

You can also use connection strings:

```csharp
IBlobStorage storage = StorageFactory.Blobs.FromConnectionString("azure.datalake.gen1://account=...;tenantId=...;principalId=...;principalSecret=...;listBatchSize=...");
```

the last parameter *listBatchSize* is optional and defaults to `5000`.

## Azure Data Lake Gen 2

<img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/azure-data-lake.png" width="128" align="right"></img> 
In order to use Azure DataLake Gen 2, reference [![NuGet](https://img.shields.io/nuget/v/FluentStorage.Azure.Blobs.svg)](https://www.nuget.org/packages/FluentStorage.Azure.Blobs/) package first.

Gen 2 is the new generation of the storage API, and you should always prefer it to Gen 1 accounts when you can. Both Gen 1 and Gen 2 providers are located in the same NuGet package.

Gen 2 provider is 100% compatible with hierarchical namespaces. When you use blob path, the first part of the path is filesystem name, i.e. `storage.WriteTextAsync("filesystem/folder/subfolder/.../file.extension`. Apparently you cannot create files in the root folder, they always need to be prefixed with filesystem name.

If filesystem doesn't exist, we will try to create it for you, if the account provided has enough permissions to do so.

#### Authentication

You can authenticate in the ways described below. To use connection strings, don't forget to call `StorageFactory.Modules.UseAzureDataLake()` somewhere when your program starts.

##### Using **Shared Key Authentication**

```csharp
IBlobStorage storage = StorageFactory.Blobs.AzureDataLakeGen2StoreBySharedAccessKey(
   accountName,
   sharedKey);
```

or

```csharp
IBlobStorage storage = StorageFactory.Blobs.FromConnectionString(
   "azure.datalake.gen2://account=...;key=...");
```

##### Using **Service Principal**

```csharp
IBlobStorage storage = StorageFactory.Blobs.AzureDataLakeGen2StoreByClientSecret(
   accountName,
   tenantId,
   principalId,
   principalSecret);
```

or

```csharp
IBlobStorage storage = StorageFactory.Blobs.FromConnectionString(
   "azure.datalake.gen2://account=...;tenantId=...;principalId=...;principalSecret=...");
```

##### Using **Managed Service Identity**

```csharp
IBlobStorage storage = StorageFactory.Blobs.AzureDataLakeGen2StoreByManagedIdentity(
   accountName);
```

or

```csharp
IBlobStorage storage = StorageFactory.Blobs.FromConnectionString(
   "azure.datalake.gen2://account=...;msi");
```

#### Permissions Management

ADLS Gen 2 [supports](https://docs.microsoft.com/en-us/azure/storage/blobs/data-lake-storage-access-control) RBAC and [POSIX](https://www.usenix.org/legacy/publications/library/proceedings/usenix03/tech/freenix03/full_papers/gruenbacher/gruenbacher_html/main.html) like permissions on both file and folder level. FluentStorage fully supports permissions management on those and exposes simplified easy-to-use API to drive them.

Because permission management is ADLS Gen 2 specific feature, you cannot use `IBlobStorage` interface, however you can cast it to `IAzureDataLakeGen2BlobStorage` which in turn implements `IBlobStorage` as well.

In order to get permissions for an object located on a specific path, you can call the API:

```csharp
IBlobStorage genericStorage = StorageFactory.Blobs.AzureDataLakeGen2StoreByClientSecret(name, key);
IAzureDataLakeGen2BlobStorage gen2Storage = (IAzureDataLakeGen2BlobStorage)genericStorage;

//get permissions
AccessControl access = await _storage.GetAccessControlAsync(path);
```

`AccessControl` is a self explanatory structure that contains information about owning user, owning group, their permissions, and any custom ACL entries assigned to this object.

In order to set permissions, you need to call `SetAccessControlAsync` passing back modified `AccessControl` structure. Let's say I'd like to add *write* access to a user with ID `6b157067-78b0-4478-ba7b-ade5c66f1a9a` (Active Directory Object ID). I'd write code like this (using the structure we've just got back from `GetAccessControlAsync`):

```csharp
// add user to custom ACL
access.Acl.Add(new AclEntry(ObjectType.User, userId, false, true, false));

//update the ACL on Gen 2 storage
await _storage.SetAccessControlAsync(path, access);
```