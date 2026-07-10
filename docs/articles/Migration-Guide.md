## Migrating from FluentStorage v7 to v8+

### Removed packages

|     | Package | Reason |
|-----|---------|--------|
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/databricks.png" width="32"></img>| **[FluentStorage.Databricks](https://www.nuget.org/packages/FluentStorage.Databricks)**  | We will no longer maintain this package because DBFS is not a mainstream storage backend for a storage abstraction library. |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/azure-event-hub.png" width="32"></img>| **[FluentStorage.Azure.EventHub](https://www.nuget.org/packages/FluentStorage.Azure.EventHub)** | Due to low community usage, we will no longer maintain this library. |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/azure-data-lake.png" width="32"></img>| **[FluentStorage.Azure.DataLake](https://www.nuget.org/packages/FluentStorage.Azure.DataLake)** | We are no longer maintaining this package as it only caters to DataLake Gen 1, which has been superseded by DataLake Gen2. Gen1 is considered a legacy service and is no longer the direction Microsoft recommends for new development. |
| <img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/azure-service-fabric.png" width="32"></img>| **[FluentStorage.Azure.ServiceFabric](https://www.nuget.org/packages/FluentStorage.Azure.ServiceFabric)** | We will no longer maintain this package because ServiceFabric is not a first-class object storage or messaging service, which is outside our current scope, and it also has extremely low community usage. |

## Migrating from Storage.NET

### Packaging changes

Change your **NuGet packages** and your **imports** using this mapping:

| Old name                                          | New name               |
| ------------------------------------------------- | --------------------------------- |
| Storage.Net                                       | [FluentStorage](https://www.nuget.org/packages/FluentStorage)       |
| Storage.Net.Amazon.Aws                            | [FluentStorage.AWS](https://www.nuget.org/packages/FluentStorage.AWS)                 |
| Storage.Net.Gcp.CloudStorage                      | [FluentStorage.GCP](https://www.nuget.org/packages/FluentStorage.GCP)                 |
| Storage.Net.Databricks                            | No longer supported          |
| Storage.Net.Ftp                                   | [FluentStorage.FTP](https://www.nuget.org/packages/FluentStorage.FTP)                 |
| Storage.Net.Microsoft.Azure.Storage.Blobs         | [FluentStorage.Azure.Blobs](https://www.nuget.org/packages/FluentStorage.Azure.Blobs)         |
| Storage.Net.Microsoft.Azure.Storage.Files         | [FluentStorage.Azure.Files](https://www.nuget.org/packages/FluentStorage.Azure.Files)         |
| Storage.Net.Microsoft.Azure.EventHub              | No longer supported      |
| Storage.Net.Microsoft.Azure.ServiceBus            | [FluentStorage.Azure.ServiceBus](https://www.nuget.org/packages/FluentStorage.Azure.ServiceBus)    |
| Storage.Net.Microsoft.Azure.KeyVault              | [FluentStorage.Azure.KeyVault](https://www.nuget.org/packages/FluentStorage.Azure.KeyVault)      |
| Storage.Net.Microsoft.Azure.ServiceFabric         | No longer supported |
| Storage.Net.Microsoft.Azure.Queues                | [FluentStorage.Azure.Queues](https://www.nuget.org/packages/FluentStorage.Azure.Queues)        |
| Storage.Net.Microsoft.Azure.DataLake.Storage.Gen1 | No longer supported      |

### Encryption changes

We now accept the IV and Key in the constructors of the `*EncryptionSink` classes.

Since most of the time we will be using Dependency Injection (DI) to instantiate our objects, each time the `SymmetricEncryptionSink`, the IV is set

```
		public AesSymmetricEncryptionSink(string key) {
			_cryptoAlgorithm = Aes.Create();
			_cryptoAlgorithm.Key = Convert.FromBase64String(key);
			_cryptoAlgorithm.GenerateIV();
		}
```

This means that if a blob is stored, then it can only be unencrypted using the same instance of the Sink or with an instance of the Sink with the same IV (secret key)

If you are storing to AWS/Azure storage, then if you try and retrieve the blob at another time, you can't read the blob even though the original key might be the same, the secret generated at the time of encryption is no longer the secret that the sink is trying to use the decrypt.

Storage.Net has got this wrong and has used a pattern like this, which creates a transient class, however we [fixed it](https://github.com/robinrodricks/FluentStorage/pull/36) to accept the IV and Key in the constructors of the sinks.