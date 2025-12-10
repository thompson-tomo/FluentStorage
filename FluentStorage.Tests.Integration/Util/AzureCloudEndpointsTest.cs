using System;
using Xunit;
using Azure.Identity;
using FluentStorage.Azure.Blobs;

namespace FluentStorage.Tests.Integration.Util {
	public class AzureCloudEndpointsTest {

		[Theory]
		[InlineData(AzureCloudEnvironment.Global, "core.windows.net")]
		[InlineData(AzureCloudEnvironment.China, "core.chinacloudapi.cn")]
		[InlineData(AzureCloudEnvironment.USGovernment, "core.usgovcloudapi.net")]
		[InlineData(AzureCloudEnvironment.Germany, "core.cloudapi.de")]
		public void GetBlobEndpoint_VariousEnvironments_ReturnsExpected(AzureCloudEnvironment env, string expected) {
			string result = AzureCloudEndpoints.GetBlobEndpoint(env);
			Assert.Equal(expected, result);
		}

		[Theory]
		[InlineData(AzureCloudEnvironment.Global, "dfs.core.windows.net")]
		[InlineData(AzureCloudEnvironment.China, "dfs.core.chinacloudapi.cn")]
		[InlineData(AzureCloudEnvironment.USGovernment, "dfs.core.usgovcloudapi.net")]
		[InlineData(AzureCloudEnvironment.Germany, "dfs.core.cloudapi.de")]
		public void GetDataLakeEndpoint_VariousEnvironments_ReturnsExpected(AzureCloudEnvironment env, string expected) {
			string result = AzureCloudEndpoints.GetDataLakeEndpoint(env);
			Assert.Equal(expected, result);
		}

		[Theory]
		[InlineData(AzureCloudEnvironment.Global)]
		[InlineData(AzureCloudEnvironment.China)]
		[InlineData(AzureCloudEnvironment.USGovernment)]
		[InlineData(AzureCloudEnvironment.Germany)]
		public void GetAuthorityEndpoint_VariousEnvironments_ReturnsExpectedUris(AzureCloudEnvironment env) {
			Uri result = AzureCloudEndpoints.GetAuthorityEndpoint(env);

			Uri expected = env switch {
				AzureCloudEnvironment.China => AzureAuthorityHosts.AzureChina,
				AzureCloudEnvironment.USGovernment => AzureAuthorityHosts.AzureGovernment,
				AzureCloudEnvironment.Germany => AzureAuthorityHosts.AzureGermany,
				_ => AzureAuthorityHosts.AzurePublicCloud
			};

			Assert.Equal(expected, result);
		}
	}
}
