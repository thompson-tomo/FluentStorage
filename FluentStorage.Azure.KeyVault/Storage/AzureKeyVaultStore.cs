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
	public class AzureKeyVaultStore : StoreBase {
		private readonly SecretClient _client;
		private readonly string _vaultUri;
		private static readonly Regex secretNameRegex = new Regex("^[0-9a-zA-Z-]+$");

		public AzureKeyVaultStore(Uri vaultUri, TokenCredential tokenCredential) {
			_client = new SecretClient(vaultUri, tokenCredential);

			_vaultUri = vaultUri.ToString().Trim('/');
		}


		/// <summary>
		/// Returns the SecretClient instance for this store.
		/// </summary>
		public override async Task<object> GetClient() {
			return _client;
		}
		public async Task<List<StoreObject>> ListObjects(StorageListOptions options, CancellationToken cancellationToken) {
			if (options == null) options = new StorageListOptions();

			GenericValidation.CheckBlobPrefix(options.FilePrefix);

			if (!StoragePath.IsRootPath(options.FolderPath)) return new List<StoreObject>();

			var secrets = new List<StoreObject>();

			await foreach (SecretProperties secretProperties in _client.GetPropertiesOfSecretsAsync(cancellationToken).ConfigureAwait(false)) {
				StoreObject blob = ToBlob(secretProperties);
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

		private static StoreObject ToBlob(SecretProperties secretProperties) {
			var blob = new StoreObject(secretProperties.Name, StorageObjectType.File);
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

		public override async Task SetObject(string fullPath, Stream dataStream, string contentType, bool append, CancellationToken cancellationToken) {
			GenericValidation.CheckBlobFullPath(fullPath);
			fullPath = NormaliseSecretName(fullPath);
			if (append) throw new ArgumentException("appending to secrets is not supported", nameof(append));

			byte[] data = dataStream.ToByteArray();
			string value = Encoding.UTF8.GetString(data);
			await _client.SetSecretAsync(fullPath, value, cancellationToken).ConfigureAwait(false);
		}
		public override async Task SetObject(string fullPath, Stream dataStream, bool append, CancellationToken cancellationToken) {
			await SetObject(fullPath, dataStream, null, append, cancellationToken).ConfigureAwait(false);
		}

		public override async Task<Stream> OpenRead(string fullPath, CancellationToken cancellationToken) {
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

		public override async Task DeleteObjects(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			GenericValidation.CheckBlobFullPaths(fullPaths);

			await Task.WhenAll(fullPaths.Select(fullPath => DeleteObject(fullPath, cancellationToken))).ConfigureAwait(false);
		}

		public override async Task DeleteObject(string fullPath, CancellationToken cancellationToken) {
			fullPath = NormaliseSecretName(fullPath);

			try {
				await _client.StartDeleteSecretAsync(fullPath, cancellationToken).ConfigureAwait(false);
			}
			catch (RequestFailedException ex) when (ex.Status == 404) {

			}
		}

		public override async Task<List<bool>> ObjectsExists(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			GenericValidation.CheckBlobFullPaths(fullPaths);

			return (await Task.WhenAll(fullPaths.Select(fullPath => ObjectExists(fullPath))).ConfigureAwait(false)).ToList();
		}

		public override async Task<bool> ObjectExists(string fullPath, CancellationToken cancellationToken = default) {
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

		public override async Task<List<StoreObject>> GetObjectsInfo(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			GenericValidation.CheckBlobFullPaths(fullPaths);

			return (await Task.WhenAll(fullPaths.Select(fullPath => GetBlobAsync(fullPath))).ConfigureAwait(false)).ToList();
		}

		private async Task<StoreObject> GetBlobAsync(string fullPath) {
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

	}
}
