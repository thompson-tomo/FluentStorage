<img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/ftp.png" width="128" align="right"></img> To use this, you need to reference [![NuGet](https://img.shields.io/nuget/v/FluentStorage.FTP.svg)](https://www.nuget.org/packages/FluentStorage.FTP/) package first, which wraps [FluentFTP](https://github.com/robinrodricks/FluentFTP).

You can use this package to connect to FTP and FTPS servers.

The provider respects folder structure of the remote FTP share.

## Connect to FTP Server

You can instantiate it either by using a simple helper method accepting the most basic parameters, like hostname, username and password, however for custom scenarios you can always construct your own instance of `FtpClient` from the FluentFTP library and pass it to FluentStorage to manage:

```csharp
IBlobStorage storage = StorageFactory.Blobs.Ftp("myhost.com", new NetworkCredential("username", "password"));

// specify a custom ftp port (12345) as an example, real world scenarios may need extra customisations
var client = new FtpClient("myhost.com", 12345, new NetworkCredential("username", "password"));
IBlobStorage storage = StorageFactory.Blobs.FtpFromFluentFtpClient(client);
```

## Connection Strings
To create from connection string, first register the module when your program starts by calling `StorageFactory.Modules.UseFtpStorage();` then use the following connections tring:

```csharp
IBlobStorage storage = StorageFactory.Blobs.FromConnectionString("ftp://host=hostname;user=username;password=password");
```

