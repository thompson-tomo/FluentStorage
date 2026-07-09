using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using FluentStorage.AWS.Blobs;
using FluentStorage.Storage;
using Xunit;

namespace FluentStorage.Tests.Integration.AWS {
	[Trait("Category", "Blobs")]
	public class LeakyAmazonS3StorageTest {
		private readonly ITestSettings _settings;
		private readonly IS3Storage _storage;

		public LeakyAmazonS3StorageTest() {
			_settings = Settings.Instance;

			_storage = (IS3Storage)AwsS3Storage.FromCredentials(
			   _settings.AwsAccessKeyId, _settings.AwsSecretAccessKey, null, _settings.AwsTestBucketName, _settings.AwsTestBucketRegion);
		}
	}
}
