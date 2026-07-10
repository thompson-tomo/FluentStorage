<img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/sftp.png" width="128" align="right"></img> To use this, you need to reference [![NuGet](https://img.shields.io/nuget/v/FluentStorage.SFTP.svg)](https://www.nuget.org/packages/FluentStorage.SFTP/) package first, which wraps [SSH.NET](https://github.com/sshnet/SSH.NET/).

The provider respects folder structure of the remote SFTP share.

## Connect to SFTP Server
You can instantiate it either by using a simple helper method accepting the most basic parameters, like hostname, port, username and password, however for custom scenarios you can always construct your own instance of `SftpClient` from the SSH.NET library and pass it to FluentStorage to manage:

```csharp
IBlobStorage storage = StorageFactory.Blobs.Sftp("myhost.com", "username", "password");

// With a custom port and a private key file
IBlobStorage storage = StorageFactory.Blobs.Sftp("myhost.com", 2222, "username", new PrivateKeyFile("filename"));

// With both password and public-key authentication as an example, real world scenarios may need extra customisations
var connectionInfo = new ConnectionInfo("sftp.foo.com", "guest", new PasswordAuthenticationMethod("guest", "pwd"), new PrivateKeyAuthenticationMethod("rsa.key"));
IBlobStorage storage = StorageFactory.Blobs.Sftp(connectionInfo);
```

## Connection Strings
To create from connection string, first register the module when your program starts by calling `StorageFactory.Modules.UseSftpStorage();` then use the following connections tring:

```csharp
IBlobStorage storage = StorageFactory.Blobs.FromConnectionString("sftp://host=hostname;user=username;password=password");
```