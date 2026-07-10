<img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/azure-queue-storage.png" width="128" align="right"></img> In order to use Azure Queue Storage reference [![NuGet](https://img.shields.io/nuget/v/FluentStorage.Azure.Queues.svg)](https://www.nuget.org/packages/FluentStorage.Azure.Queues) package first.


```csharp
IMessagePublisher publisher = StorageFactory.Messages.AzureStorageQueuePublisher();

IMessageReceiver receiver = StorageFactory.Messages.AzureStorageQueueReceiver();
```

```csharp
IMessagePublisher publisher = StorageFactory.Messages.PublisherFromConnectionString("azure.queue://account=...;key=...;queue=...");

IMessageReceiver receiver = StorageFactory.Messages.ReceiverFromConnectionString("azure.queue://account=..;key=...;queue=...");
```
