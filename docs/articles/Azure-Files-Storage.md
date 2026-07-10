In order to use Microsoft Azure blob or file storage you need to reference [![NuGet](https://img.shields.io/nuget/v/FluentStorage.Azure.Blobs.svg)](https://www.nuget.org/packages/FluentStorage.Azure.Blobs/).

## Connect to Azure Files
<img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/azure-blob-file.png" width="128" align="right"></img> 

To create file share by storage name and key:

```csharp
IBlobStorage storage = StorageFactory.Blobs.AzureFiles(accountName, accountKey);
IBlobStorage storage = StorageFactory.Blobs.AzureFiles(accountName, accountKey, serviceUri);
```

To create Azure Files with Shared Key:

```csharp
IBlobStorage storage = StorageFactory.Blobs.AzureFilesWithSharedKey(accountName, accountKey);
IBlobStorage storage = StorageFactory.Blobs.AzureFilesWithSharedKey(accountName, accountKey, cloudEnvironment);
IBlobStorage storage = StorageFactory.Blobs.AzureFilesWithSharedKey(accountName, accountKey, serviceUri, cloudEnvironment);
```

To create Azure Files with Azure AD (optionally with AD Authority endpoint):

```csharp
IBlobStorage storage = StorageFactory.Blobs.AzureFilesWithAzureAd(accountName, tenantId, applicationId, applicationSecret);
IBlobStorage storage = StorageFactory.Blobs.AzureFilesWithAzureAd(accountName, tenantId, applicationId, applicationSecret, cloudEnvironment);
IBlobStorage storage = StorageFactory.Blobs.AzureFilesWithAzureAd(accountName, tenantId, applicationId, applicationSecret, activeDirectoryAuthEndpoint);
IBlobStorage storage = StorageFactory.Blobs.AzureFilesWithAzureAd(accountName, tenantId, applicationId, applicationSecret, activeDirectoryAuthEndpoint, cloudEnvironment);
```

To create Azure Files with Token Credentials:

```csharp
IBlobStorage storage = StorageFactory.Blobs.AzureFilesWithTokenCredential(accountName, tokenCredential);
IBlobStorage storage = StorageFactory.Blobs.AzureFilesWithTokenCredential(accountName, tokenCredential, cloudEnvironment);
```

To create Azure Files with Managed Identity:

```csharp
IBlobStorage storage = StorageFactory.Blobs.AzureFilesWithMsi(accountName);
IBlobStorage storage = StorageFactory.Blobs.AzureFilesWithMsi(accountName, cloudEnvironment);
IBlobStorage storage = StorageFactory.Blobs.AzureFilesWithMsi(accountName, clientId);
IBlobStorage storage = StorageFactory.Blobs.AzureFilesWithMsi(accountName, clientId, cloudEnvironment);
```

## Connection Strings

To create with connection string, first register the module when your program starts by calling `StorageFactory.Modules.UseAzureBlobStorage();` then use the following:

```csharp
IBlobStorage storage = StorageFactory.Blobs.FromConnectionString("azure.file://account=account_name;key=secret_value");
```

To create an instance of Azure Files that wraps around the native SDK `ShareServiceClient` (use the native option with caution)

```csharp
IBlobStorage storage = StorageFactory.Blobs.AzureFiles(client);
```

## Native Operations

You can access some native, blob storage specific operations by casting (unsafe) `IBlobStorage` to `IAzureBlobStorage`.

See [this article](Azure-Blob-Storage.md#native-operations) for more info.