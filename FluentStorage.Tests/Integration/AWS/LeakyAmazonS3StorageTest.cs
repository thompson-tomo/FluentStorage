using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using FluentStorage.AWS.Storage;
using FluentStorage.Storage;
using FluentStorage.Tests.Integration.Config;
using Xunit;

namespace FluentStorage.Tests.Integration.AWS {
	
	public class LeakyAmazonS3StorageTest {
		private readonly TestConfig _settings;
		private readonly IS3Storage _storage;

		public LeakyAmazonS3StorageTest() {
			_settings = TestConfigLoader.Config;

			_storage = (IS3Storage)AwsS3Storage.FromCredentials(
			   _settings.AwsAccessKey, _settings.AwsSecretKey, null, _settings.AwsBucket, _settings.AwsBucketRegion);
		}
	}
}
