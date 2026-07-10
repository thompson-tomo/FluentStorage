using Google.Apis.Auth.OAuth2;
using FluentStorage.Storage;
using FluentStorage.Gcp.CloudStorage;
using FluentStorage.Gcp.CloudStorage.Storage;
using System;
using FluentStorage.Utils.Extensions;

namespace FluentStorage {
	/// <summary>
	/// Google Cloud Storage factory that is accessible using `FluentStorage.StorageFactory.Blobs` by way of extension methods.
	/// </summary>
	public static class GoogleCloudStorage {
		/// <summary>
		/// Initialises Google Cloud Storage module required for connection strings to work
		/// </summary>
		/// <param name="factory"></param>
		/// <returns></returns>
		public static void Use() {
			FluentStorage.StorageFactory.Use(new Module());
		}

		/// <summary>
		/// Creates a Google Cloud Storage storage instance, where credentials have to be configured
		/// in an environment variable as officially described at https://cloud.google.com/storage/docs/reference/libraries#setting_up_authentication
		/// </summary>
		/// <param name="factory"></param>
		/// <param name="bucketName">Storage bucket name</param>
		/// <returns></returns>
		public static GoogleCloudStore FromEnvironmentVariable(
		   string bucketName) {
			return new GoogleCloudStore(bucketName);
		}

		/// <summary>
		/// Creates a Google Cloud Storage storage instance, where credentials are located in an external json file.
		/// </summary>
		/// <param name="factory"></param>
		/// <param name="bucketName"></param>
		/// <param name="credentialsFilePath">Path to a json file containing credentials.</param>
		/// <returns></returns>
		public static GoogleCloudStore FromJsonFile(
		   string bucketName,
		   string credentialsFilePath) {
			GoogleCredential cred = GoogleCredential.FromFile(credentialsFilePath);

			return new GoogleCloudStore(bucketName, cred);
		}

		/// <summary>
		/// Creates a Google Cloud Storage storage instance, where credentials are passed as a json string
		/// </summary>
		/// <param name="factory"></param>
		/// <param name="bucketName"></param>
		/// <param name="credentialsJsonString">Json string containing credentials.</param>
		/// <param name="isBase64EncodedString">When true, <paramref name="credentialsJsonString"/> is bas64 encoded</param>
		/// <returns></returns>
		public static GoogleCloudStore FromJson(
		   string bucketName,
		   string credentialsJsonString,
		   bool isBase64EncodedString = false) {
			string json = isBase64EncodedString ? credentialsJsonString.Base64Decode() : credentialsJsonString;

			GoogleCredential cred = GoogleCredential.FromJson(json);

			return new GoogleCloudStore(bucketName, cred);
		}
	}
}
