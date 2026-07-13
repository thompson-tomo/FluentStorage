using FluentStorage.AWS.Storage;
using FluentStorage.Storage;

namespace FluentStorage.AWS.Factory {
	/// <summary>
	/// MinIO storage factory to create instances of `IBucket` using this provider.
	/// </summary>
	public static class MinIOStorage {

		/// <summary>
		/// Creates an MinIO storage provider (S3-compatible).
		/// </summary>
		/// <param name="accessKeyId">Access key ID</param>
		/// <param name="secretAccessKey">Secret access key</param>
		/// <param name="bucketName">Bucket name</param>
		/// <param name="awsRegion">AWS Region name (like "us-east-1")</param>
		/// <param name="minioServerUrl">MinIO Server URL</param>
		/// <param name="sessionToken">Optional. Only required when using session credentials.</param>
		/// <returns>A reference to the created storage</returns>

		public static IStore FromCredentials(
			string accessKeyId,
			string secretAccessKey,
			string bucketName,
			string awsRegion,
			string minioServerUrl,
			string sessionToken = null) {
			return S3Store.FromMinIO(accessKeyId, secretAccessKey, bucketName, awsRegion, minioServerUrl, sessionToken);
		}
	}
}
