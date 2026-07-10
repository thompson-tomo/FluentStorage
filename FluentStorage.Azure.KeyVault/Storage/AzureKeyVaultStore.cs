using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using FluentStorage.Storage;
using Azure.Security.KeyVault.Secrets;
using Azure.Core;
using Azure;
using FluentStorage.Utils.Extensions;
using FluentStorage.Enums;

namespace FluentStorage.Azure.KeyVault.Storage {
	public class AzureKeyVaultStore : BucketBase {
		private readonly SecretClient _client;
		private readonly string _vaultUri;
		private static readonly Regex secretNameRegex = new Regex("^[0-9a-zA-Z-]+$");

		public AzureKeyVaultStore(Uri vaultUri, TokenCredential tokenCredential) {
			_client = new SecretClient(vaultUri, tokenCredential);

			_vaultUri = vaultUri.ToString().Trim('/');
		}


		public async Task<List<StorageObject>> ListAsync(StorageListOptions options, CancellationToken cancellationToken) {
			if (options == null) options = new StorageListOptions();

			GenericValidation.CheckBlobPrefix(options.FilePrefix);

			if (!StoragePath.IsRootPath(options.FolderPath)) return new List<StorageObject>();

			var secrets = new List<StorageObject>();

			await foreach (SecretProperties secretProperties in _client.GetPropertiesOfSecretsAsync(cancellationToken).ConfigureAwait(false)) {
				StorageObject blob = ToBlob(secretProperties);
				if (!options.IsMatch(blob))
					continue;

				if (options.BrowseFilter != null && !options.BrowseFilter(blob))
					continue;

				secrets.Add(blob);

				if (options.MaxResults != null && secrets.Count >= options.MaxResults.Value)
					break;
			}

			return secrets;
		}

		private static StorageObject ToBlob(SecretProperties secretProperties) {
			var blob = new StorageObject(secretProperties.Name, StorageObjectType.File);
			blob.DateModified = secretProperties.UpdatedOn;

			blob.TryAddProperties(
			   "ContentType", secretProperties.ContentType,
			   "CreatedOn", secretProperties.CreatedOn,
			   "IsEnabled", secretProperties.Enabled,
			   "ExpiresOn", secretProperties.ExpiresOn,
			   "Id", secretProperties.Id,
			   "KeyId", secretProperties.KeyId,
			   "IsManaged", secretProperties.Managed,
			   "NotBefore", secretProperties.NotBefore,
			   "RecoveryLevel", secretProperties.RecoveryLevel,
			   "Tags", secretProperties.Tags,
			   "UpdatedOn", secretProperties.UpdatedOn,
			   "VaultUri", secretProperties.VaultUri,
			   "Version", secretProperties.Version,
			   "IsSecret", true);

			return blob;
		}

		public async Task WriteAsync(string fullPath, Stream dataStream, bool append, CancellationToken cancellationToken) {
			GenericValidation.CheckBlobFullPath(fullPath);
			fullPath = NormaliseSecretName(fullPath);
			if (append) throw new ArgumentException("appending to secrets is not supported", nameof(append));

			byte[] data = dataStream.ToByteArray();
			string value = Encoding.UTF8.GetString(data);
			await _client.SetSecretAsync(fullPath, value, cancellationToken).ConfigureAwait(false);
		}
		public async Task WriteAsync(string fullPath, Stream dataStream, string contentType, bool append, CancellationToken cancellationToken) {
			await WriteAsync(fullPath, dataStream, null, append, cancellationToken).ConfigureAwait(false);
		}

		public async Task<Stream> OpenReadAsync(string fullPath, CancellationToken cancellationToken) {
			GenericValidation.CheckBlobFullPath(fullPath);
			fullPath = NormaliseSecretName(fullPath);

			try {
				Response<KeyVaultSecret> secret = await _client.GetSecretAsync(fullPath, cancellationToken: cancellationToken).ConfigureAwait(false);

				string value = secret.Value.Value;

				return value.ToMemoryStream();
			}
			catch (RequestFailedException ex) when (ex.Status == 404) {
				return null;
			}
		}

		public async Task DeleteAsync(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			GenericValidation.CheckBlobFullPaths(fullPaths);

			await Task.WhenAll(fullPaths.Select(fullPath => DeleteAsync(fullPath, cancellationToken))).ConfigureAwait(false);
		}

		private async Task DeleteAsync(string fullPath, CancellationToken cancellationToken) {
			fullPath = NormaliseSecretName(fullPath);

			try {
				await _client.StartDeleteSecretAsync(fullPath, cancellationToken).ConfigureAwait(false);
			}
			catch (RequestFailedException ex) when (ex.Status == 404) {

			}
		}

		public async Task<List<bool>> ExistsAsync(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			GenericValidation.CheckBlobFullPaths(fullPaths);

			return (await Task.WhenAll(fullPaths.Select(fullPath => ExistsAsync(fullPath))).ConfigureAwait(false)).ToList();
		}

		private async Task<bool> ExistsAsync(string fullPath) {
			GenericValidation.CheckBlobFullPath(fullPath);

			fullPath = NormaliseSecretName(fullPath);

			try {
				await _client.GetSecretAsync(fullPath).ConfigureAwait(false);
			}
			catch (RequestFailedException ex) when (ex.Status == 404) {
				return false;
			}

			return true;
		}

		public async Task<List<StorageObject>> GetBlobsAsync(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			GenericValidation.CheckBlobFullPaths(fullPaths);

			return (await Task.WhenAll(fullPaths.Select(fullPath => GetBlobAsync(fullPath))).ConfigureAwait(false)).ToList();
		}

		public Task SetBlobsAsync(IEnumerable<StorageObject> blobs, CancellationToken cancellationToken = default) {
			throw new NotSupportedException();
		}

		private async Task<StorageObject> GetBlobAsync(string fullPath) {
			fullPath = NormaliseSecretName(fullPath);

			try {
				Response<KeyVaultSecret> secret = await _client.GetSecretAsync(fullPath).ConfigureAwait(false);

				return ToBlob(secret.Value.Properties);
			}
			catch (RequestFailedException ex) when (ex.Status == 404) {
				return null;
			}
		}


		private static string NormaliseSecretName(string fullPath) {
			fullPath = StoragePath.Normalize(fullPath).Substring(1);

			if (!secretNameRegex.IsMatch(fullPath)) {
				throw new NotSupportedException($"secret '{fullPath}' does not match expected pattern '^[0-9a-zA-Z-]+$'");
			}

			return fullPath;
		}

		public void Dispose() {
		}

	}
}
