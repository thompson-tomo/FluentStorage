## Providers

- [AWS S3 Storage](AWS-S3-Storage.md)
- [Cloudflare R2 Storage](Cloudflare-R2-Storage.md)
- [MinIO Storage](MinIO-Storage.md)
- [DigitalOcean Spaces Storage](DigitalOcean-Spaces-Storage.md)
- [Wasabi Storage](Wasabi-Storage.md)
- [Backblaze B2 Storage](Backblaze-B2-Storage.md)
- [Hetzner Storage](Hetzner-Storage.md)
- [Vultr Storage](Vultr-Storage.md)
- [Google Cloud Storage](Google-Cloud-Storage.md)
- [Azure Blob Storage](Azure-Blob-Storage.md)
- [Azure File Storage](Azure-Files-Storage.md)
- [Azure Data Lake](Azure-Data-Lake.md)
- [Azure Key Vault](Azure-Key-Vault.md)
- [In-Memory, Local Disk & ZIP](Standard-Storage.md)
- [FTP & FTPS Storage](FTP-Storage.md)
- [SFTP Storage](SFTP-Storage.md)

## API

All of the core methods are defined in the `IBlobStorage` interface. This is the interface that's enough for a new storage provider to implement in order to add a new storage provider.

However, some providers support more than just basic operations, for instance Azure Blob Storage supports blob leasing, shared access signatures etc., therefore it actually implements `IAzureBlobStorage` interface that in turn implements `IBlobStorage` interface, and extends the functionality further. Same goes for AWS S3 and others.

## Getting Started

Blob Storage stores files. A file has only two properties - `ID` and raw data. If you build an analogy with disk filesystem, file ID is a file name.

Blob Storage is really simple abstraction - you read or write file data by it's ID, nothing else.

The entry point to a blog storage is [IBucket](xref:FluentStorage.Storage.IBucket) interface. This interface is small but contains all possible methods to work with blobs, such as uploading and downloading data, listing storage contents, deleting files etc. The interface is kept small so that new storage providers can be added easily, without implementing a plethora of interface methods.

In addition to this interface, there are plency of extension methods which enrich the functionality, therefore you will see more methods than this interface actually declares. They add a lot of useful and functionality rich methods to work with storage. For instance, `IBlobStorage` upload functionality only works with streams, however extension methods allows you to upload text, stream, file or even a class as a blob. Extension methods are also provider agnostic, therefore all the rich functionality just works and doesn't have to be reimplemented in underlying data provider.

All the storage implementations can be created either directly or using factory methods available in the `FluentStorage.StorageFactory.Blobs` class. More methods appear in that class as you reference a **NuGet** package containing specific implementations, however there are a few built-in implementations available out of the box as well. After referencing an appropriate package from NuGet you can call to a storage factory to create a respective storage implementation.

You can also use connection strings to create blob storage instances. Connection strings are often useful if you want to completely abstract yourself from the underlying implementation. Please read the appropriate implementation details for connection string details. For instance, to create an instance of Azure Blob Storage provider you could write:

```csharp
IBlobStorage storage = StorageFactory.Blobs.FromConnectionString("azure.blobs://...parameters...");
```

In this example we create a blob storage implementation which happens to be Microsoft Azure blob storage. The project is referencing an appropriate [nuget package](https://www.nuget.org/packages/FluentStorage.Microsoft.Azure.Storage.Blobs/). As blob storage methods promote streaming we create a `MemoryStream` over a string for simplicity sake. In your case the actual stream can come from a variety of sources.

```csharp
public async Task BlobStorage_sample2()
{
    IBlobStorage storage = StorageFactory.Blobs.AzureBlobStorageWithSharedKey(
		"storage name",
		"storage key");

    //upload it
    await storage.WriteTextAsync("mycontainer/someid", "test content");

    //read back
    string content = await storage.ReadTextAsync("mycontainer/someid");
}
```
