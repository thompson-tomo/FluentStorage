<img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/aws-sqs.png" width="128" align="right"></img> In order to use [AWS Simple Queue Service](https://aws.amazon.com/sqs/) you need to reference
[![NuGet](https://img.shields.io/nuget/v/FluentStorage.AWS.svg)](https://www.nuget.org/packages/FluentStorage.AWS/) first. The provider wraps around the standard AWS SDK.

To construct a publisher use the following:

```csharp
IMessagePublisher queuePublisher = StorageFactory.Messages.AmazonSQSMessagePublisher(
  accessKeyId,
  secretAccessKey,
  serviceUrl,
  queueName,
  regionEndpoint);

IMessagePublisher topicPublisher = StorageFactory.Messages.AmazonSQSMessageReceiver(
  accessKeyId,
  secretAccessKey,
  serviceUrl,
  queueName,
  regionEndpoint);
```

- **accessKeyId** and **secretAccessKey*) are credentials to access the queue.
- **serviceUrl** indicates the service URL, for instance `https://sqs.us-east-1.amazonaws.com`
- **queueName** is the name of the queue
- **retionEndpoint** is optional and defaults to `USEast1`
