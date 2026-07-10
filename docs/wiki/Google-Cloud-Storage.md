<img src="https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/providers/gcp.png" width="128" align="right"></img> In order to use [Google Cloud Storage](https://cloud.google.com/storage/) reference [![NuGet](https://img.shields.io/nuget/v/FluentStorage.GCP.svg)](https://www.nuget.org/packages/FluentStorage.GCP) package first.

You definitely want to use FluentStorage for working with Google Storage, as it solves quite a few issues which are hard to manage with the raw SDK:

- Listing of files and folders
- Recursive and non-recursive listing
- Upload and download operations are continuing to be optimised


## Connect using Service Account IAM 

You can configure Service Account IAM permissions for accessing GCS buckets, and then simply omit the `cred` in the connection string, and then FluentStorage will fallback to connecting using the Workload Identity setup.

You can then use [Application Default Credentials](https://cloud.google.com/docs/authentication/application-default-credentials) (ADC), which is the recommended authentication approach for workloads running on Google Cloud and even for local development via                                                                                                                                                                                                                                                                                                                                                                                                                         `gcloud auth application-default login`.


## Connect using environment variables

If you have credentials stored in an environment variable. (As described [here](https://cloud.google.com/storage/docs/reference/libraries#setting_up_authentication))

```csharp
IBlobStorage storage = StorageFactory.Blobs.GoogleCloudStorageFromEnvironmentVariable(bucketName);
```


## Connect using credentials file

If you have credentials stored in an external JSON file.

```csharp
IBlobStorage storage = StorageFactory.Blobs.GoogleCloudStorageFromJsonFile(bucketName, filePath);
```


## Connect using credentials

If you need the credentials passed as a JSON string.

```csharp
IBlobStorage storage = StorageFactory.Blobs.GoogleCloudStorageFromJson(
   bucketName, credentialsJsonString, isBase64EncodedString = false);
```

The last parameter says whether the string is base64 encoded or not, which is handy if credentials are stored in some sort of config file.



## Connection Strings

First, don't forget to initialise the module:

```csharp
StorageFactory.Modules.UseGoogleCloudStorage();
```

Then, use the string:

```csharp
IBlobStorage storage = StorageFactory.Blobs.FromConnectionString("google.storage://bucket=...;cred=...");
```

Where **cred** is a *BASE64* encoded credential string.
