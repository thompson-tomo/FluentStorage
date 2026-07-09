using System;
using Amazon;
using FluentStorage.Azure.ServiceBus;
using FluentStorage.Storage;
using FluentStorage.Queue;
using Xunit;
using FluentStorage.AWS.Factory;

namespace FluentStorage.Tests.Integration.Messaging {

	#region [ Azure Storage Queue ]

	public class AzureStorageQueueFixture : MessagingFixture {
		protected override IQueue CreateMessenger(ITestSettings settings) =>
		   AzureQueueStorage.FromCredentials(settings.AzureStorageName,settings.AzureStorageKey);
	}

	public class AzureStorageQueueTest : MessagingTest, IClassFixture<AzureStorageQueueFixture> {
		public AzureStorageQueueTest(AzureStorageQueueFixture fixture) : base(fixture) {
		}
	}

	#endregion

	#region [ In-Memory ]

	public class InMemoryFixture : MessagingFixture {
		protected override IQueue CreateMessenger(ITestSettings settings) {
			return QueueFactory.InMemory("test");
		}
	}

	public class InMemoryTest : MessagingTest, IClassFixture<InMemoryFixture> {
		public InMemoryTest(InMemoryFixture fixture) : base(fixture) {
		}
	}

	#endregion

	#region [ Disk ]

	public class DiskFixture : MessagingFixture {
		protected override IQueue CreateMessenger(ITestSettings settings) {
			return QueueFactory.Disk(_testDir);
		}
	}

	public class DiskTest : MessagingTest, IClassFixture<DiskFixture> {
		public DiskTest(DiskFixture fixture) : base(fixture) {
		}
	}

	#endregion

	#region [ AWS SQS ]

	public class AwsSQSFixture : MessagingFixture {
		protected override IQueue CreateMessenger(ITestSettings settings) {
			return AwsSqsStorage.FromCredentials(
			   settings.AwsAccessKeyId,
			   settings.AwsSecretAccessKey,
			   "https://sqs.us-east-1.amazonaws.com",
			   RegionEndpoint.USEast1);
		}
	}


	public class AwsSQSTest : MessagingTest, IClassFixture<AwsSQSFixture> {
		public AwsSQSTest(AwsSQSFixture fixture) : base(fixture) {
		}
	}
	#endregion

	#region [ Azure Service Bus ]

	public class AzureServiceBusFixture : MessagingFixture {
		protected override IQueue CreateMessenger(ITestSettings settings) {
			return AzureServiceBus.FromConnectionString(settings.AzureServiceBusConnectionString);

		}
	}

	public class AzureServiceBusTopicTest : MessagingTest, IClassFixture<AzureServiceBusFixture> {
		public AzureServiceBusTopicTest(AzureServiceBusFixture fixture) : base(fixture, "t/", "t/fxtopic", receiveChannelSuffix: "/default") {
		}
	}

	public class AzureServiceBusSubscriptionTest : MessagingTest, IClassFixture<AzureServiceBusFixture> {
		public AzureServiceBusSubscriptionTest(AzureServiceBusFixture fixture) : base(fixture, "t/", "t/fxtopic/fxsubscription", receiveChannelSuffix: "/default") {
		}
	}


	public class AzureServiceBusQueueTest : MessagingTest, IClassFixture<AzureServiceBusFixture> {
		public AzureServiceBusQueueTest(AzureServiceBusFixture fixture) : base(fixture, "q/", "q/fxqueue") {
		}
	}

	#endregion

}
