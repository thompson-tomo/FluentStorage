## Nugets

To get started, add the main [FluentStorage](https://www.nuget.org/packages/FluentStorage) Nuget package into your .NET application.

Then add the [packages you need](https://github.com/robinrodricks/FluentStorage#packages) as per the cloud storage providers you want to use.

## Connect to blob storage

To construct storage classes, you need to use the factory methods.

| Package                                                                 | Factory method                                                   | Purpose                                                                                               |
| ----------------------------------------------------------------------- | ---------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| [Package](https://nuget.org/packages/FluentStorage)                     | `StorageFactory.Blobs.FromConnectionString`                      | Creates a blob storage instance from a connection string                                              |
| [Package](https://nuget.org/packages/FluentStorage)                     | `StorageFactory.Blobs.DirectoryFiles`                            | Creates a storage for a specific disk directory                                                       |
| [Package](https://nuget.org/packages/FluentStorage)                     | `StorageFactory.Blobs.InMemory`                                  | Creates a storage which stores everyting in memory                                                    |
| [Package](https://nuget.org/packages/FluentStorage)                     | `StorageFactory.Blobs.Virtual`                                   | Creates a virtual storage where you can mount other storage providers to a specific virtual directory |
| [Package](https://nuget.org/packages/FluentStorage.AWS)                 | `StorageFactory.Blobs.AwsS3`                                     | Creates an AWS S3 storage bucket or custom S3-compatible storage server.                              |
| [Package](https://nuget.org/packages/FluentStorage.AWS)                 | `StorageFactory.Blobs.DigitalOceanSpaces`                        | Creates a [DigitalOcean Spaces](https://www.digitalocean.com/products/spaces) storage (S3 compatible).  |
| [Package](https://nuget.org/packages/FluentStorage.AWS)                 | `StorageFactory.Blobs.MinIO`                                     | Creates a [MinIO](https://min.io/) storage server (S3 compatible).    |
| [Package](https://nuget.org/packages/FluentStorage.AWS)                 | `StorageFactory.Blobs.Wasabi`                                     | Creates a [Wasabi](https://wasabi.com/) storage (S3 compatible).  |
| [Package](https://nuget.org/packages/FluentStorage.GCP)                 | `StorageFactory.Blobs.GoogleCloudStorageFromEnvironmentVariable` | Creates a Google Cloud Storage storage with credentials in environment variables                      |
| [Package](https://nuget.org/packages/FluentStorage.GCP)                 | `StorageFactory.Blobs.GoogleCloudStorageFromJsonFile`            | Creates a Google Cloud Storage storage with credentials in an external JSON                           |
| [Package](https://nuget.org/packages/FluentStorage.GCP)                 | `StorageFactory.Blobs.GoogleCloudStorageFromJson`                | Creates a Google Cloud Storage storage with credentials in a passed JSON string                       |
| [Package](https://nuget.org/packages/FluentStorage.Azure.Blobs)         | `StorageFactory.Blobs.AzureBlobStorageWithLocalEmulator`         | Creates Azure Blob Storage to connect to a local emulator                                             |
| [Package](https://nuget.org/packages/FluentStorage.Azure.Blobs)         | `StorageFactory.Blobs.AzureBlobStorageWithSharedKey`             | Creates Azure Blob Storage with Shared key authentication                                             |
| [Package](https://nuget.org/packages/FluentStorage.Azure.Blobs)         | `StorageFactory.Blobs.AzureBlobStorageWithAzureAd`               | Creates Azure Blob Storage with Azure ActiveDirectory (AAD) authentication                            |
| [Package](https://nuget.org/packages/FluentStorage.Azure.Blobs)         | `StorageFactory.Blobs.AzureBlobStorageWithTokenCredential`       | Creates Azure Blob Storage with token credentials                                                     |
| [Package](https://nuget.org/packages/FluentStorage.Azure.Blobs)         | `StorageFactory.Blobs.AzureBlobStorageWithSas`                   | Creates Azure Blob Storage with SAS identity                                                          |
| [Package](https://nuget.org/packages/FluentStorage.Azure.Blobs)         | `StorageFactory.Blobs.AzureBlobStorageWithMsi`                   | Creates Azure Blob Storage with Managed Identity                                                      |
| [Package](https://nuget.org/packages/FluentStorage.Azure.DataLake)      | `StorageFactory.Blobs.AzureDataLakeGen1StoreByClientSecret`      | Creates Azure Data Lake Gen 1 Store client                                                            |
| [Package](https://nuget.org/packages/FluentStorage.Azure.Blobs)         | `StorageFactory.Blobs.AzureDataLakeStorageWithMsi`               | Creates Azure Data Lake Gen 2 Storage with Managed Identity                                           |
| [Package](https://nuget.org/packages/FluentStorage.Azure.Blobs)         | `StorageFactory.Blobs.AzureDataLakeStorageWithSharedKey`         | Creates Azure Data Lake Gen 2 Storage with Shared key authentication                                  |
| [Package](https://nuget.org/packages/FluentStorage.Azure.Blobs)         | `StorageFactory.Blobs.AzureDataLakeStorageWithAzureAd`           | Creates Azure Data Lake Gen 2 Storage with Azure ActiveDirectory (AAD) authentication                 |
| [Package](https://nuget.org/packages/FluentStorage.Azure.Files)         | `StorageFactory.Blobs.AzureFiles`                                | Creates Azure Files storage                                                                           |
| [Package](https://nuget.org/packages/FluentStorage.Azure.KeyVault)      | `StorageFactory.Blobs.AzureKeyVault`                             | Creates Azure Key Vault secrets storage                                                               |
| [Package](https://nuget.org/packages/FluentStorage.Azure.KeyVault)      | `StorageFactory.Blobs.AzureKeyVaultWithMsi`                      | Creates Azure Key Vault secrets with Managed Identity                                                 |
| [Package](https://nuget.org/packages/FluentStorage.Azure.ServiceFabric) | `StorageFactory.Blobs.AzureServiceFabricReliableStorage`         | Creates Azure Service Fabric storage                                                                  |
| [Package](https://nuget.org/packages/FluentStorage.Databricks)          | `StorageFactory.Blobs.Databricks`                                | Creates Azure Databricks DBFS storage                                                                 |
| [Package](https://nuget.org/packages/FluentStorage.FTP)                 | `StorageFactory.Blobs.Ftp`                                       | Creates an interface to FTP/FTPS servers                                                              |
| [Package](https://nuget.org/packages/FluentStorage.FTP)                 | `StorageFactory.Blobs.FtpFromFluentFtpClient`                    | Creates an interface to FTP/FTPS servers with the given client instance                               |
| [Package](https://nuget.org/packages/FluentStorage.SFTP)                | `StorageFactory.Blobs.Sftp`                                      | Creates an interface to SFTP servers (FTP over SSH)                                                   |


## Connect to message queues

To construct storage classes, you need to use the factory methods.

| Package                                                              | Factory method                                          | Purpose                                                                        |
| -------------------------------------------------------------------- | ------------------------------------------------------- | ------------------------------------------------------------------------------ |
| [Package](https://nuget.org/packages/FluentStorage)                  | `StorageFactory.Messages.MessengerFromConnectionString` | Creates a message publisher from connection string                             |
| [Package](https://nuget.org/packages/FluentStorage)                  | `StorageFactory.Messages.InMemory`                      | Creates a message publisher which holds messages in memory                     |
| [Package](https://nuget.org/packages/FluentStorage)                  | `StorageFactory.Messages.Disk`                          | Creates a message publisher that uses local disk directory as a backing store. |
| [Package](https://nuget.org/packages/FluentStorage.AWS)              | `StorageFactory.Messages.AwsSQS`                        | Creates Amazon Simple Queue Service publisher.                                 |
| [Package](https://nuget.org/packages/FluentStorage.Azure.EventHub)   | `StorageFactory.Messages.AzureEventHub`                 | Create Azure Event Hub messenger by full connection string.                    |
| [Package](https://nuget.org/packages/FluentStorage.Azure.Queues)     | `StorageFactory.Messages.AzureStorageQueue`             | Creates a message publisher to Azure Storage Queues.                           |
| [Package](https://nuget.org/packages/FluentStorage.Azure.ServiceBus) | `StorageFactory.Messages.AzureServiceBus`               | Creates a message publisher ho Azure Service Bus Queue.                        |
| [Package](https://nuget.org/packages/FluentStorage.Azure.ServiceBus) | `StorageFactory.Messages.AzureServiceBusTopicReceiver`  | Creates a message reciever from Azure Service Bus Queue.                       |
