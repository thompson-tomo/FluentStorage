<p align="center">
<img src="https://github.com/robinrodricks/FluentStorage/raw/develop/.github/logo.png" alt="FluentStorage" />
</p>

<p align="center">
<a href="https://www.nuget.org/packages/FluentStorage"><img src="https://img.shields.io/nuget/vpre/FluentStorage.svg" alt="Version" /></a>
<a href="https://www.nuget.org/packages/FluentStorage"><img src="https://img.shields.io/nuget/dt/FluentStorage.svg" alt="Downloads" /></a>
<a href="https://github.com/robinrodricks/FluentStorage/graphs/contributors"><img src="https://img.shields.io/github/contributors/robinrodricks/FluentStorage.svg" alt="GitHub contributors" /></a>
<a href="https://github.com/robinrodricks/FluentStorage/blob/develop/LICENSE"><img src="https://img.shields.io/github/license/robinrodricks/FluentStorage.svg" alt="License" /></a>
</p>

<p align="center">
    <b>FluentStorage is free, but powered by</b> <a href="https://github.com/sponsors/robinrodricks"><b>your donations</b></a>
</p>

### One Interface To Rule Them All

FluentStorage is a fully managed polycloud .NET cloud storage library, optimized for speed. It helps you interface with multiple cloud providers from a [single unified API](#polycloud-api), allowing you to switch cloud providers or support multiple cloud providers without any logic changes.

It provides a [single unified API](#polycloud-api) for [Object storage](https://github.com/robinrodricks/FluentStorage/wiki/Blob-Storage) and [Queue messaging](https://github.com/robinrodricks/FluentStorage/wiki/Message-Storage) across all [cloud storage providers](#storage-providers) like AWS S3, AWS SQS, GCP Storage, FTP, FTPS, SFTP, Azure Blob, Azure Files, Azure Queue, Azure Service Bus, Azure Data Lake, Azure Key Vault, Cloudflare R2, DigitalOcean Spaces, MinIO, Wasabi, Backblaze B2, Hetzner, Vultr. Each provider has its own [Nuget package](#packages) with zero configuration required.

It provides extensive Object manipulation commands, File uploads/downloads, Directory listing, File permissions/CHMOD, UTF-8 support, Async/await support and more. Its API is fully asynchronous and has identical behavior across all providers. It also implements [in-memory and on-disk storage providers](https://github.com/robinrodricks/FluentStorage/wiki/Standard-Storage), so you can test on a local machine.

It is written entirely in C#, with no external dependencies. FluentStorage is released under the permissive MIT License, so it can be used in both proprietary and free/open source applications.



## Storage Providers

FluentStorage supports the following cloud storage providers:

|       		| Documentation Link                                               | Main interface | Store class |  `GetClient()` returns
|---------------| --------------------------------------------------------------------------- | ---------------- | ---------------- | ---------------- | 
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/aws.png" width="32"></img>| [AWS S3](https://github.com/robinrodricks/FluentStorage/wiki/AWS-S3-Storage#connect-to-aws-s3)   |  `IStore` | `S3Store` |`AmazonS3Client`  |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/azure.png" width="32"></img>|  [Azure Blobs](https://github.com/robinrodricks/FluentStorage/wiki/Azure-Blob-Storage) | `IAzureBlobStore` | `AzureBlobStore` | `BlobServiceClient` |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/azure.png" width="32"></img>| [Azure Files](https://github.com/robinrodricks/FluentStorage/wiki/Azure-Files-Storage) 	| `IStore` | `AzureFilesStore` | `ShareServiceClient` |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/azure-data-lake.png" width="32"></img>| [Azure DataLake](https://github.com/robinrodricks/FluentStorage/wiki/Azure-Data-Lake) 	| `IStore` | `AzureDataLakeStore` | --- |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/gcp.png" width="32"></img>| [GCP](https://github.com/robinrodricks/FluentStorage/wiki/Google-Cloud-Storage)         			| `IStore` | `GoogleCloudStore` | `StorageClient` |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/alibaba.png" width="32"></img>|  [Alibaba OSS](https://github.com/robinrodricks/FluentStorage/wiki/Alibaba-OSS-Storage)         | `IStore` | `S3Store` |`OssClient`  |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/minio.png" width="32"></img>| [MinIO](https://github.com/robinrodricks/FluentStorage/wiki/MinIO-Storage)         				| `IStore` | `S3Store` |`AmazonS3Client`  |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/r2.png" width="32"></img>| [Cloudflare R2](https://github.com/robinrodricks/FluentStorage/wiki/Cloudflare-R2-Storage)  			| `IStore` | `S3Store` |`AmazonS3Client`  |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/digitalocean.png" width="32"></img>|[DigitalOcean Spaces](https://github.com/robinrodricks/FluentStorage/wiki/DigitalOcean-Spaces-Storage)  |`IStore` | `S3Store` |`AmazonS3Client`  |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/wasabi.png" width="32"></img>| [Wasabi](https://github.com/robinrodricks/FluentStorage/wiki/Wasabi-Storage)         			| `IStore` | `S3Store` |`AmazonS3Client`  |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/backblaze.png" width="32"></img>|  [Backblaze B2](https://github.com/robinrodricks/FluentStorage/wiki/Backblaze-B2-Storage)  	| `IStore` | `S3Store` |`AmazonS3Client`  |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/hetzner.png" width="32"></img>|  [Hetzner](https://github.com/robinrodricks/FluentStorage/wiki/Hetzner-Storage)  				|`IStore` | `S3Store` |`AmazonS3Client`  |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/vultr.png" width="32"></img>|  [Vultr](https://github.com/robinrodricks/FluentStorage/wiki/Vultr-Storage)        				| `IStore` | `S3Store` |`AmazonS3Client`  |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/ftp.png" width="32"></img>|  [FTP](https://github.com/robinrodricks/FluentStorage/wiki/FTP-Storage)        						| `IStore` | `FtpStore` |  `AsyncFtpClient` |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/sftp.png" width="32"></img>|  [SFTP](https://github.com/robinrodricks/FluentStorage/wiki/SFTP-Storage)        					| `IStore` | `SftpStore` | `SftpClient` |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/local.png" width="32"></img>|  [Local Disk](https://github.com/robinrodricks/FluentStorage/wiki/Standard-Storage)        		| `IStore` | `DiskStore` | --- |

To add support for a new provider, search for `[ADD STORAGE PROVIDER]` across all code files and make the required changes.

## Polycloud API

This table shows the API supported by `IStore` across various cloud and server providers.

<table>
<thead>

<tr>
<th>API</th>
<th> <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/azure.png" width="32"></img> <br><b>Azure<br>Blobs</b></th>
<th> <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/azure.png" width="32"></img> <br><b>Azure<br>Files</b></th>
<th> <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/aws.png" width="32"></img> <br><b>AWS S3</b></th>
<th> <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/gcp.png" width="32"></img> <br><b>GCP</b></th>
<th> <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/alibaba.png" width="32"></img> <br><b>Alibaba</b></th>
<th> <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/ftp.png" width="32"></img> <br><b>FTP</b></th>
<th> <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/sftp.png" width="32"></img> <br><b>SFTP</b></th>
<th> <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/local.png" width="32"></img> <br><b>Local<br>Disk</b></th>
</tr>

</thead>
<tbody>

<tr>
<td colspan="9"><b>System</b></td>
</tr>
<tr>
<td>&nbsp;&nbsp; GetClient</td>
<td>✔️</td><td>✔️</td><td>✔️<br></td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>❌</td>
</tr>
<tr>
<td>&nbsp;&nbsp; GetServer</td>
<td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>✔️</td><td>✔️</td><td>❌</td>
</tr>

<tr>
<td colspan="9"><b>File listing</b></td>
</tr>
<tr>
<td>&nbsp;&nbsp; ListDirectory</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td>
</tr>
<tr>
<td>&nbsp;&nbsp; ListObjects</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td>
</tr>

<tr>
<td colspan="9"><b>File upload/download</b></td>
</tr>
<tr>
<td>&nbsp;&nbsp; GetObject</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td>
</tr>
<tr>
<td>&nbsp;&nbsp; SetObject</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td>
</tr>
<tr>
<td>&nbsp;&nbsp; GetBytes</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td>
</tr>
<tr>
<td>&nbsp;&nbsp; SetBytes</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td>
</tr>
<tr>
<td>&nbsp;&nbsp; DownloadObject</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td>
</tr>
<tr>
<td>&nbsp;&nbsp; UploadObject</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td>
</tr>
<tr>
<td>&nbsp;&nbsp; OpenRead</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td>
</tr>
<tr>
<td>&nbsp;&nbsp; OpenWrite</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td>
</tr>

<tr>
<th>API</th>
<th><b>Azure<br>Blobs</b></th>
<th><b>Azure<br>Files</b></th>
<th><b>AWS S3</b></th>
<th><b>GCP</b></th>
<th><b>Alibaba</b></th>
<th><b>FTP</b></th>
<th><b>SFTP</b></th>
<th><b>Local<br>Disk</b></th>
</tr>

<tr>
<td colspan="9"><b>File manipulation</b></td>
</tr>
<tr>
<td>&nbsp;&nbsp; ObjectExists</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td>
</tr>
<tr>
<td>&nbsp;&nbsp; MoveObject</td>
<td>✔️</td><td>❌</td><td>✔️</td><td>❌</td><td>✔️</td><td>✔️</td><td>✔️</td><td>❌</td>
</tr>
<tr>
<td>&nbsp;&nbsp; DeleteObject</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td>
</tr>
<tr>
<td>&nbsp;&nbsp; DeleteObjects</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td>
</tr>
<tr>
<td>&nbsp;&nbsp; GetObjectInfo</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td>
</tr>
<tr>
<td>&nbsp;&nbsp; GetObjectsInfo</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td>
</tr>
<tr>
<td>&nbsp;&nbsp; SetObjectInfo</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>❌</td><td>❌</td><td>✔️</td>
</tr>
<tr>
<td>&nbsp;&nbsp; SetObjectsInfo</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>❌</td><td>❌</td><td>✔️</td>
</tr>
<tr>
<td>&nbsp;&nbsp; GetFilePermissions</td>
<td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>✔️</td><td>✔️</td><td>❌</td>
</tr>
<tr>
<td>&nbsp;&nbsp; SetFilePermissions</td>
<td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>✔️</td><td>✔️</td><td>❌</td>
</tr>

<tr>
<td colspan="9"><b>Directory manipulation</b></td>
</tr>
<tr>
<td>&nbsp;&nbsp; DirectoryExists</td>
<td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>✔️</td><td>✔️</td><td>✔️</td>
</tr>
<tr>
<td>&nbsp;&nbsp; CreateDirectory</td>
<td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>✔️</td><td>✔️</td><td>✔️</td>
</tr>
<tr>
<td>&nbsp;&nbsp; DeleteDirectory</td>
<td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td>
</tr>
<tr>
<td>&nbsp;&nbsp; MoveDirectory</td>
<td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>✔️</td><td>✔️</td><td>✔️</td>
</tr>


<tr>
<th>API</th>
<th><b>Azure<br>Blobs</b></th>
<th><b>Azure<br>Files</b></th>
<th><b>AWS S3</b></th>
<th><b>GCP</b></th>
<th><b>Alibaba</b></th>
<th><b>FTP</b></th>
<th><b>SFTP</b></th>
<th><b>Local<br>Disk</b></th>
</tr>

<tr>
<td colspan="9"><b>Presigned URL generation</b></td>
</tr>
<tr>
<td>&nbsp;&nbsp; GetUploadUrl</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>❌</td><td>❌</td><td>❌</td>
</tr>
<tr>
<td>&nbsp;&nbsp; GetDownloadUrl</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>❌</td><td>❌</td><td>❌</td>
</tr>
<tr>
<td>&nbsp;&nbsp; GetPresignedUrl</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>❌</td><td>❌</td><td>❌</td>
</tr>
<tr>
<td>&nbsp;&nbsp; GetObjectSas</td>
<td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>✔️</td><td>❌</td><td>❌</td><td>❌</td>
</tr>

<tr>
<td colspan="9"><b>Security and access control</b></td>
</tr>
<tr>
<td>&nbsp;&nbsp; SetAcl</td>
<td>❌</td><td>❌</td><td>✔️</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td>
</tr>
<tr>
<td>&nbsp;&nbsp; AcquireLease</td>
<td>✔️</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td>
</tr>
<tr>
<td>&nbsp;&nbsp; BreakLease</td>
<td>✔️</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td>
</tr>
<tr>
<td>&nbsp;&nbsp; GetContainerPublicAccess</td>
<td>✔️</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td>
</tr>
<tr>
<td>&nbsp;&nbsp; SetContainerPublicAccess</td>
<td>✔️</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td>
</tr>
<tr>
<td>&nbsp;&nbsp; GetStorageSas</td>
<td>✔️</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td>
</tr>
<tr>
<td>&nbsp;&nbsp; GetContainerSas</td>
<td>✔️</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td><td>❌</td>
</tr>

</tbody>
</table>

## Packages

Stable binaries are released on NuGet, and contain everything you need to use Cloud Storage in your .NET app.


|       		| Package      		| Latest Version	|  Downloads	|  Documentation	|
|---------------|---------------		|-----------	|-----------		|-----------		|
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/local.png" width="32"></img>| **[FluentStorage](https://www.nuget.org/packages/FluentStorage)**      	|     [![Version](https://img.shields.io/nuget/vpre/FluentStorage.svg)](https://www.nuget.org/packages/FluentStorage) 		|  [![Downloads](https://img.shields.io/nuget/dt/FluentStorage.svg)](https://www.nuget.org/packages/FluentStorage) | [Standard](https://github.com/robinrodricks/FluentStorage/wiki/Standard-Storage) |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/aws.png" width="32"></img>| **[FluentStorage.AWS](https://www.nuget.org/packages/FluentStorage.AWS)**      	|     [![Version](https://img.shields.io/nuget/vpre/FluentStorage.AWS.svg)](https://www.nuget.org/packages/FluentStorage.AWS) 		|  [![Downloads](https://img.shields.io/nuget/dt/FluentStorage.AWS.svg)](https://www.nuget.org/packages/FluentStorage.AWS) | [S3](https://github.com/robinrodricks/FluentStorage/wiki/AWS-S3-Storage), [SQS](https://github.com/robinrodricks/FluentStorage/wiki/AWS-SQS) |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/gcp.png" width="32"></img>| **[FluentStorage.GCP](https://www.nuget.org/packages/FluentStorage.GCP)**      	|     [![Version](https://img.shields.io/nuget/vpre/FluentStorage.GCP.svg)](https://www.nuget.org/packages/FluentStorage.GCP) 		|  [![Downloads](https://img.shields.io/nuget/dt/FluentStorage.GCP.svg)](https://www.nuget.org/packages/FluentStorage.GCP) | [GCP](https://github.com/robinrodricks/FluentStorage/wiki/Google-Cloud-Storage) |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/azure.png" width="32"></img>| **[FluentStorage.Azure](https://www.nuget.org/packages/FluentStorage.Azure)**      	|     [![Version](https://img.shields.io/nuget/vpre/FluentStorage.Azure.svg)](https://www.nuget.org/packages/FluentStorage.Azure) 		|  [![Downloads](https://img.shields.io/nuget/dt/FluentStorage.Azure.svg)](https://www.nuget.org/packages/FluentStorage.Azure) | --- |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/azure-blob-block.png" width="32"></img>| **[FluentStorage.Azure.Blobs](https://www.nuget.org/packages/FluentStorage.Azure.Blobs)**      	|     [![Version](https://img.shields.io/nuget/vpre/FluentStorage.Azure.Blobs.svg)](https://www.nuget.org/packages/FluentStorage.Azure.Blobs) 		|  [![Downloads](https://img.shields.io/nuget/dt/FluentStorage.Azure.Blobs.svg)](https://www.nuget.org/packages/FluentStorage.Azure.Blobs) | [Blob](https://github.com/robinrodricks/FluentStorage/wiki/Azure-Blob-Storage) |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/azure-blob-file.png" width="32"></img>| **[FluentStorage.Azure.Files](https://www.nuget.org/packages/FluentStorage.Azure.Files)**      	|     [![Version](https://img.shields.io/nuget/vpre/FluentStorage.Azure.Files.svg)](https://www.nuget.org/packages/FluentStorage.Azure.Files) 		|  [![Downloads](https://img.shields.io/nuget/dt/FluentStorage.Azure.Files.svg)](https://www.nuget.org/packages/FluentStorage.Azure.Files) | [File](https://github.com/robinrodricks/FluentStorage/wiki/Azure-Files-Storage) |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/azure-service-bus.png" width="32"></img>| **[FluentStorage.Azure.ServiceBus](https://www.nuget.org/packages/FluentStorage.Azure.ServiceBus)**      	|     [![Version](https://img.shields.io/nuget/vpre/FluentStorage.Azure.ServiceBus.svg)](https://www.nuget.org/packages/FluentStorage.Azure.ServiceBus) 		|  [![Downloads](https://img.shields.io/nuget/dt/FluentStorage.Azure.ServiceBus.svg)](https://www.nuget.org/packages/FluentStorage.Azure.ServiceBus) | [ServiceBus](https://github.com/robinrodricks/FluentStorage/wiki/Azure-Service-Bus) |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/azure-key-vault.png" width="32"></img>| **[FluentStorage.Azure.KeyVault](https://www.nuget.org/packages/FluentStorage.Azure.KeyVault)**      	|     [![Version](https://img.shields.io/nuget/vpre/FluentStorage.Azure.KeyVault.svg)](https://www.nuget.org/packages/FluentStorage.Azure.KeyVault) 		|  [![Downloads](https://img.shields.io/nuget/dt/FluentStorage.Azure.KeyVault.svg)](https://www.nuget.org/packages/FluentStorage.Azure.KeyVault) | [KeyVault](https://github.com/robinrodricks/FluentStorage/wiki/Azure-Key-Vault) |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/azure-queue-storage.png" width="32"></img>| **[FluentStorage.Azure.Queues](https://www.nuget.org/packages/FluentStorage.Azure.Queues)**      	|     [![Version](https://img.shields.io/nuget/vpre/FluentStorage.Azure.Queues.svg)](https://www.nuget.org/packages/FluentStorage.Azure.Queues) 		|  [![Downloads](https://img.shields.io/nuget/dt/FluentStorage.Azure.Queues.svg)](https://www.nuget.org/packages/FluentStorage.Azure.Queues) | [Queue](https://github.com/robinrodricks/FluentStorage/wiki/Azure-Queue-Storage) |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/alibaba.png" width="32"></img>| **[FluentStorage.Alibaba](https://www.nuget.org/packages/FluentStorage.Alibaba)**      	|     [![Version](https://img.shields.io/nuget/vpre/FluentStorage.Alibaba.svg)](https://www.nuget.org/packages/FluentStorage.Alibaba) 		|  [![Downloads](https://img.shields.io/nuget/dt/FluentStorage.Alibaba.svg)](https://www.nuget.org/packages/FluentStorage.Alibaba) | [Alibaba](https://github.com/robinrodricks/FluentStorage/wiki/Alibaba-OSS-Storage) |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/ftp.png" width="32"></img>| **[FluentStorage.FTP](https://www.nuget.org/packages/FluentStorage.FTP)**      	|     [![Version](https://img.shields.io/nuget/vpre/FluentStorage.FTP.svg)](https://www.nuget.org/packages/FluentStorage.FTP) 		|  [![Downloads](https://img.shields.io/nuget/dt/FluentStorage.FTP.svg)](https://www.nuget.org/packages/FluentStorage.FTP) | [FTP](https://github.com/robinrodricks/FluentStorage/wiki/FTP-Storage) |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/sftp.png" width="32"></img>| **[FluentStorage.SFTP](https://www.nuget.org/packages/FluentStorage.SFTP)**      	|     [![Version](https://img.shields.io/nuget/vpre/FluentStorage.SFTP.svg)](https://www.nuget.org/packages/FluentStorage.SFTP) 		|  [![Downloads](https://img.shields.io/nuget/dt/FluentStorage.SFTP.svg)](https://www.nuget.org/packages/FluentStorage.SFTP) | [SFTP](https://github.com/robinrodricks/FluentStorage/wiki/SFTP-Storage) |



## Platform Support

FluentStorage works on .NET and .NET Standard/.NET Core.

| Platform      		| Binaries Folder	|
|---------------		|-----------		|
| **.NET 7.0**      	| net70     		|
| **.NET 8.0**      	| net80     		|
| **.NET 9.0**      	| net90     		|
| **.NET Standard 2.0** | netstandard2.0	|
| **.NET Standard 2.1** | netstandard2.1	|

FluentStorage is also supported on these platforms: (via .NET Standard)

  - **Mono** 4.6
  - **Xamarin.iOS** 10.0
  - **Xamarin.Android** 10.0
  - **Universal Windows Platform** 10.0

Binaries for all platforms are built from a single Visual Studio Project. You will need the latest [Visual Studio](https://visualstudio.microsoft.com/downloads/) to build or contribute to FluentStorage.




## Architecture

### Without FluentStorage

Today, most cloud applications and services are developed against vendor-specific APIs like Amazon S3 API or Azure Blob API for cloud storage capabilities.

Using multiple vendor-specific APIs can increase your vendor lock-in, and makes your application more complex and harder to maintain. And sometimes these APIs may not offer all the functionality you need for your application. Polycloud also becomes very hard to implement.

![Arch](https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/arch-without.png)

### With FluentStorage

What if we had a single, consistent API to deal with all types of cloud storage? That would solve these issues and bring more flexibility in switching cloud providers or cloud services.

Thus was born the idea for FluentStorage.

You can use a single, consistent API to interact with multiple cloud providers, where each provider is supported through its own special Nuget package.

![Arch](https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/arch-with.png)



## Benefits

1. Easy to write code that is not tied to a specific cloud provider.

2. Easily switch between different providers without having to rewrite any part of their application or service.

3. Easily migrate to using a new storage technology for some part of your cloud application (S3 buckets, Azure Blobs, FTP, etc.)

4. Natively implement polycloud (support for multiple public clouds)





## Documentation

Check the [Wiki](https://github.com/robinrodricks/FluentStorage/wiki).



## What's New

In 2026, we added:

* **Version 8** with a massive redesign of the [entire API and behaviour](https://github.com/robinrodricks/FluentStorage/wiki/Migration-Guide)
* **Alibaba OSS** provider
* **Cloudflare R2** provider
* **Backblaze B2** provider
* **Hetzner** provider
* **Vultr** provider
* **Azure.Identity support for Azure Files** with token credential, client secret, and managed identity authentication.
* **Wiki** pages per provider
* **Removed** unused packages: Databricks, EventHub, DataLake Gen 1, ServiceFabric

In 2025, we added:

* **Azure.Identity support for Azure Blobs** with token credential, client secret, and managed identity authentication.

In 2024, we added:

* **DigitalOcean Spaces** provider
* **MinIO** provider
* **Wasabi** provider

In 2023, we added:

* **SFTP** provider [SSH.NET](https://github.com/sshnet/SSH.NET) added
* **FTP** provider [FluentFTP](https://github.com/robinrodricks/FluentFTP) updated to v44
* **AWS** Nuget bumped to latest versions
* **Wiki** created for documentation
* **Platform** support updated to `netstandard2.0`,`netstandard2.1`,`net50`,`net60`



## Supported Cloud Services

![Slide](https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers.svg)



## Similar Libraries

- [Foundatio](https://github.com/FoundatioFx/Foundatio) - Caching, Messaging, Queues and Storage library with AWS, Azure and many other providers
- [SlimMessageBus](https://github.com/zarusz/SlimMessageBus) - Messaging library with providers like RabbitMQ, Kafka, Azure EventHub, MQTT, Redis
- [ManagedCode.Storage](https://github.com/managedcode/Storage) - Storage library with AWS, Azure, GCP and other providers



## Sponsorship

FluentStorage has received major sponsorship from these generous organizations:

<table>
<tr>
	<td width="200px">
		<a href="https://www.microsoft.com/">
		<img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/refs/heads/develop/.github/microsoft-logo.png" /> Microsoft Corporation
		</a>
	</td>
</tr>
</table>

Has FluentStorage made a difference for you or your organization? If so, consider [becoming a sponsor](https://github.com/sponsors/robinrodricks) to help keep the project thriving. Even a small monthly contribution, like $20, can make a meaningful impact.


## Contributors

Special thanks to these awesome people who helped create FluentStorage! Shoutout to [Ivan Gavryliuk](https://github.com/aloneguid) for the original project [Storage.Net](https://github.com/aloneguid/storage).


<a href="https://github.com/robinrodricks/FluentStorage/graphs/contributors">
	<!---
	<img src="https://contributors-img.web.app/image?repo=robinrodricks/FluentStorage" />
	-->
	<img src="https://github.com/robinrodricks/FluentStorage/raw/develop/.github/contributors.png" />
</a>
