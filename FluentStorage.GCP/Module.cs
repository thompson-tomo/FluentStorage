using FluentStorage.Storage;
using FluentStorage.ConnectionStrings;
using FluentStorage.Queue;

namespace FluentStorage.Gcp.CloudStorage {
	class Module : IExternalModule, IConnectionFactory {
		public IConnectionFactory ConnectionFactory => new Module();

		public IBucket CreateBlobStorage(ConnectionString connectionString) {
			if (connectionString.Prefix == "google.storage") {
				connectionString.GetRequired("bucket", true, out string bucketName);
				string base64EncodedJson = connectionString.Get("cred");

				// When cred= is absent or empty, fall back to Application Default Credentials
				// (Workload Identity on Cloud Run, gcloud auth application-default login locally)
				if (string.IsNullOrEmpty(base64EncodedJson))
					return GoogleCloudStorage.FromEnvironmentVariable(bucketName);

				return GoogleCloudStorage.FromJson(bucketName, base64EncodedJson, true);
			}

			return null;
		}

		public IQueue CreateMessenger(ConnectionString connectionString) => null;
	}
}
