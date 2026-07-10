<img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/azure-service-bus.png" width="128" align="right"></img> 
 In order to use Microsoft Azure Service Bus you need to reference
[![NuGet](https://img.shields.io/nuget/v/FluentStorage.Azure.ServiceBus.svg)](https://www.nuget.org/packages/FluentStorage.Azure.ServiceBus/) first. The provider wraps around the new Microsoft Service Bus SDK package (`Azure.Messaging.ServiceBus`).

This provider supports both topics and queues, for publishing and receiving.

### IMessageReceiver

This new package introduces the following methods for the `IMessageReceiver` interface:

- **AzureServiceBusTopicReceiver()** - Creates Azure Service Bus Receiver for topic and subscriptions
- **AzureServiceBusQueueReceiver()** - Creates Azure Service Bus Receiver for queues

Where `ServiceBusClientOptions` and `ServiceBusProcessorOptions` are optional parameters to fine-tune the options for the `ServiceBusClient` and `ServiceBusProcessor`:

- [ServiceBusClientOptions docs](https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.servicebus.servicebusclientoptions?view=azure-dotnet)
- [ServiceBusProcessorOptions docs](https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.servicebus.servicebusprocessoroptions?view=azure-dotnet)

### IMessenger
The interface used to send messages is `IMessenger` but is a bit limited because it doesn't have the notions of queues, topics and subscriptions.

So, if you want to use the `IMessenger` interface, when passing a channel you have to use this format:
- _`q/queue name`_ for working with queues
- _`t/topic name`_ for working with topics
- _`t/topic name/subscription name`_ for working with subscriptions (as subscriptions are embedded inside topics)

All the public methods of `IMessenger` that take a channel (or channel collections) in input need to follow this naming convention.

### IAzureServiceBusMessenger

The new interface `IAzureServiceBusMessenger` extends `IMessenger`. So, if you cast your `AzureServiceBusMessenger` messenger to `IAzureServiceBusMessenger` you have access to these utility methods that target directly queues, messages, and subscriptions:

- **SendToQueueAsync()** - Sends a collection of messages to the specified queue asynchronously.
- **SendToQueueAsync()** - Sends a message to the specified queue asynchronously.
- **SendToTopicAsync()** - Sends a collection of messages to the specified topic asynchronously.
- **SendToTopicAsync()** - Sends a single message to the specified topic asynchronously.
- **SendToSubscriptionAsync()** - Sends a collection of messages to the specified topic subscription asynchronously.
- **SendToSubscriptionAsync()** - Sends a single message to the specified topic subscription asynchronously.
- **CreateQueueAsync()** - Create a new Queue in Azure ServiceBus
- **CreateTopicAsync()** - Create a new Topic in Azure ServiceBus
- **CreateSubScriptionAsync()** - Create a new Subscription in Azure ServiceBus
- **DeleteQueueAsync()** - Deletes the specified queue asynchronously.
- **DeleteSubScriptionAsyn()** - Deletes the specified topic subscription asynchronously.
- **DeleteTopicAsync()** - Deletes the specified topic asynchronously.
- **CountQueueAsync()** - Counts the number of messages in the specified queue asynchronously.
- **CountSubScriptionAsync()** - Counts the number of messages in the specified topic subscription asynchronously.
- **CountTopicAsync()** - Counts the number of messages in the specified topic asynchronously