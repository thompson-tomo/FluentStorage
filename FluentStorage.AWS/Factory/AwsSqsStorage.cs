using Amazon;
using FluentStorage.AWS.Messaging;
using FluentStorage.Queue;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluentStorage.AWS.Factory {
	public static class AwsSqsStorage {

		/// <summary>
		/// Creates Amazon Simple Queue Service publisher
		/// </summary>
		/// <param name="factory"></param>
		/// <param name="accessKeyId">Access key ID</param>
		/// <param name="secretAccessKey">Secret access key</param>
		/// <param name="serviceUrl"></param>
		/// <param name="regionEndpoint"></param>
		/// <returns></returns>
		public static IQueue FromCredentials(string accessKeyId,
		   string secretAccessKey,
		   string serviceUrl,
		   RegionEndpoint regionEndpoint = null) {
			return new SQSMessenger(accessKeyId, secretAccessKey, serviceUrl, regionEndpoint);
		}

	}
}
