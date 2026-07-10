<img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/local.png" width="128" align="right"></img> Standard storage are the providers included in the base FluentStorage nuget [![NuGet](https://img.shields.io/nuget/v/FluentStorage.svg)](https://www.nuget.org/packages/FluentStorage/).

## Standard Blob Storage

### In-Memory

In-memory provider stores blobs in process memory as array of bytes. Although it's available, we strongly discourage using it in production due to high memory fragmentation. It's sole purpose is for local testing, mocking etc.

The provider is built into FluentStorage main package.

To construct, use:

```csharp
IBlobStorage storage = StorageFactory.Blobs.InMemory();
```

which constructs a new instance of in-memory storage. Further calls to this factory create a new unique instance.

To create from connection string:

```csharp
IBlobStorage storage = StorageFactory.Blobs.FromConnectionString("inmemory://");
```

### Local Disk

Local disk providers maps a local folder to `IBlobStorage` instance, so that you can both use local disk and replicate directory structure as blob storage interface.

The provider is built into FluentStorage main package.

```csharp
IBlobStorage storage = StorageFactory.Blobs.DirectoryFiles(directory);
```

where `directory` is an instance of `System.IO.DirectoryInfo` that points to that directory. The directory does not have to exist on local disk, however it will be created on any write operation.

To create from connection string:

```csharp
IBlobStorage storage = StorageFactory.Blobs.FromConnectionString("disk://path=path_to_directory");
```

### Zip File

Zip file provider maps to a single zip archive. All the operations that include path are created as subfolders in zip archive. Archive itself doesn't need to exist, however any write operation will create a new archive and put any data you write to it.

The provider is built into FluentStorage main package as zip API are a part of .NET Standard nowadays. It is thread safe by default.

```csharp
IBlobStorage storage = StorageFactory.Blobs.ZipFile(pathToZipFile);
```

To create from connection string:

```csharp
IBlobStorage storage = StorageFactory.Blobs.FromConnectionString("zip://path=path_to_file");
```

## Standard Message Storage

### In-Memory

In-memory provider creates messaging queue directly in memory, and is useful for mocking message publisher and receiver. It's not intended to be used in production.

The provider is built into FluentStorage main package.

To construct, use:

```csharp
services.AddSingleton<FluentStorage.Messaging.IMessenger>(_ =>
    FluentStorage.StorageFactory.Messages.InMemory(queueName)
);
```

### Local Disk

Local disk messaging is backed by a local folder on disk. Every message publish call creates a new file in that folder with `.snm` extension (**S**torage **N**et **M**essage) which is a binary representation of the message.

Message receiver polls this folder every second to check for new files, get the oldest ones and transforms into `QueueMessage`. 

The provider is built into FluentStorage main package.

To construct from a connection string, use:

```csharp
IMessenger messenger = StorageFactory.Messages.MessengerFromConnectionString("disk://path=the_path");
```
