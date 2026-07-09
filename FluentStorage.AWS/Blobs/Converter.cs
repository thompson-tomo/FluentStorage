using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;

using FluentStorage.Storage;

namespace FluentStorage.AWS.Blobs {
	static class Converter {
		/// <summary>
		/// AWS prepends all the user metadata with this prefix, and all of your own keys are prepended with this automatically
		/// </summary>
		private const string MetaDataHeaderPrefix = "x-amz-meta-";

		public static async Task UpdateMetadataAsync(AmazonS3Client client, StorageObject blob, string bucketName, string key) {
			// there is no way to update metadata in S3, and the only way is to recreate it
			// however, you can copy object on top of itself (effectively a replace) and rewrite metadata, and this won't have to download the blob on the client

			var request = new CopyObjectRequest {
				SourceBucket = bucketName,
				DestinationBucket = bucketName,
				SourceKey = key,
				DestinationKey = key,
				ContentType = (string)blob.Properties["ContentType"],
				MetadataDirective = S3MetadataDirective.REPLACE
			};


			foreach (KeyValuePair<string, string> pair in blob.Metadata) {
				request.Metadata[pair.Key] = pair.Value;
			}

			await client.CopyObjectAsync(request).ConfigureAwait(false);
		}

		private static async Task AppendMetadataAsync(AmazonS3Client client, string bucketName, StorageObject blob, CancellationToken cancellationToken) {
			if (blob == null)
				return;

			GetObjectMetadataResponse obj = await client.GetObjectMetadataAsync(bucketName, blob.FullPath.Substring(1), cancellationToken).ConfigureAwait(false);

			AddMetadata(blob, obj);
		}

		public static async Task AppendMetadataAsync(AmazonS3Client client, string bucketName, IEnumerable<StorageObject> blobs, CancellationToken cancellationToken) {
			await Task.WhenAll(
			   blobs.Select(blob => AppendMetadataAsync(client, bucketName, blob, cancellationToken))).ConfigureAwait(false);
		}

		public static StorageObject ToBlob(this GetObjectMetadataResponse obj, string fullPath) {
			if (obj == null)
				return null;

			var r = new StorageObject(fullPath);
			r.MD5 = obj.ETag.Trim('\"'); //ETag contains actual MD5 hash, not sure why!
			r.Size = obj.ContentLength;
			r.LastModificationTime = obj.LastModified.Value.ToUniversalTime();
			
			AddMetadata(r, obj);

			return r;
		}

		private static void AddMetadata(StorageObject blob, GetObjectMetadataResponse response) {

			
			//add metadata and strip all
			foreach (string key in response.Metadata.Keys) {
				string value = response.Metadata[key];
				string putKey = key;
				if (putKey.StartsWith(MetaDataHeaderPrefix))
					putKey = putKey.Substring(MetaDataHeaderPrefix.Length);

				blob.Metadata[putKey] = value;
			}


			blob.Properties["ETag"] = response.ETag;

			foreach (var key in response.Headers.Keys) {
				blob.Properties[key] = response.Headers[key];
			}

		}

		public static StorageObject ToBlob(this S3Object s3Obj) {
			StorageObject blob = s3Obj.Key.EndsWith("/")
			   ? new StorageObject(s3Obj.Key, BlobItemKind.Folder)
			   //Key is an absolute path
			   : new StorageObject(s3Obj.Key, BlobItemKind.File);

			blob.Size = s3Obj.Size;
			blob.MD5 = s3Obj.ETag.Trim('\"');
			blob.LastModificationTime = s3Obj.LastModified.Value.ToUniversalTime();
			blob.Properties["StorageClass"] = s3Obj.StorageClass;
			blob.Properties["ETag"] = s3Obj.ETag;

			return blob;
		}

		public static IReadOnlyCollection<StorageObject> ToBlobs(this ListObjectsV2Response response, ListOptions options) {
			var result = new List<StorageObject>();

			//the files are listed as the S3Objects member, but they don't specifically contain folders,
			//but even if they do, they need to be filtered out

			if (response.S3Objects is not null) 
			{
				result.AddRange(
			   		response.S3Objects
				  	.Where(b => !b.Key.EndsWith("/")) //check if this is "virtual folder" as S3 console creates them (rubbish)
				  	.Select(b => b.ToBlob())
				  	.Where(options.IsMatch)
				  	.Where(b => options.BrowseFilter == null || options.BrowseFilter(b)));
			}
			//subfolders are listed in another field (what a funny name!)

			//prefix is absolute too
			// CommonPrefixes can be null https://github.com/aws/aws-sdk-net/blob/main/sdk/src/Services/S3/Generated/Model/ListObjectsV2Response.cs#L94
			if (response.CommonPrefixes is not null)
			{
			    result.AddRange(
			        response.CommonPrefixes
			            .Where(p => !StoragePath.IsRootPath(p))
			            .Select(p => new StorageObject(p, BlobItemKind.Folder)));
			}

			return result;
		}


	}
}
