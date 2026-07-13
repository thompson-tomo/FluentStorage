using FluentFTP;
using FluentFTP.Exceptions;
using FluentStorage.Enums;
using FluentStorage.Storage;

using Polly;
using Polly.Retry;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FluentStorage.FTP.Storage {
	/// <summary>
	/// Manages a single connected FTP server using FluentFTP.
	/// </summary>
	public class FtpStore : StoreBase {
		private readonly AsyncFtpClient _client;
		private readonly bool _dispose;
		private static readonly AsyncRetryPolicy retryPolicy = Policy.Handle<FtpException>().RetryAsync(3);

		public FtpStore(string hostNameOrAddress, NetworkCredential credentials, FtpDataConnectionType dataConnectionType = FtpDataConnectionType.AutoActive) {
			_client = new AsyncFtpClient(hostNameOrAddress, credentials);
			_client.Config.DataConnectionType = dataConnectionType;
			_dispose = true;
		}

		public FtpStore(AsyncFtpClient ftpClient, bool dispose = false) {
			_client = ftpClient ?? throw new ArgumentNullException(nameof(ftpClient));
			_dispose = dispose;
		}

		public override bool HasFileSystem() {
			return true;
		}
		private async Task<AsyncFtpClient> GetClient() {
			if (!_client.IsConnected) {
				await _client.Connect().ConfigureAwait(false);

				//not supported on this platform?
				//await _client.SetHashAlgorithmAsync(FtpHashAlgorithm.MD5);
			}

			return _client;
		}

		public async Task<List<StoreObject>> ListObjects(StorageListOptions options = null, CancellationToken cancellationToken = default) {
			AsyncFtpClient client = await GetClient().ConfigureAwait(false);

			if (options == null)
				options = new StorageListOptions();

			FtpListOption ftpListOption = FtpListOption.Auto;

			if (options.Recurse)
			{
				ftpListOption |= FtpListOption.Recursive;
			}

			FtpListItem[] items = await client.GetListing(options.FolderPath, ftpListOption, cancellationToken).ConfigureAwait(false);

			List<StoreObject> results = new List<StoreObject>();
			foreach (FtpListItem item in items) {
				if (options.FilePrefix != null && !item.Name.StartsWith(options.FilePrefix)) {
					continue;
				}

				StoreObject blob = ToBlobId(item);
				if (blob == null)
					continue;

				if (options.BrowseFilter != null) {
					bool include = options.BrowseFilter(blob);
					if (!include)
						continue;
				}

				results.Add(blob);

				if (options.MaxResults != null && results.Count >= options.MaxResults.Value)
					break;
			}

			return results;
		}

		private StoreObject ToBlobId(FtpListItem ff) {
			if (ff.Type != FtpObjectType.Directory && ff.Type != FtpObjectType.File)
				return null;

			StoreObject id = new StoreObject(ff.FullName,
			   ff.Type == FtpObjectType.File
			   ? StorageObjectType.File
			   : StorageObjectType.Folder);

			if (ff.RawPermissions != null) {
				id.Properties["RawPermissions"] = ff.RawPermissions;
			}

			return id;
		}

		public override async Task DeleteObjects(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			AsyncFtpClient client = await GetClient().ConfigureAwait(false);

			foreach (string path in fullPaths) {
				await DeleteObject(path, cancellationToken);
			}
		}

		public override async Task DeleteObject(string fullPath, CancellationToken cancellationToken = default) {
			AsyncFtpClient client = await GetClient().ConfigureAwait(false);

			if (await client.FileExists(fullPath, cancellationToken)) {
				await client.DeleteFile(fullPath, cancellationToken).ConfigureAwait(false);
			}
			else if (await client.DirectoryExists(fullPath, cancellationToken)) {
				await client.DeleteDirectory(fullPath, FtpListOption.Recursive, cancellationToken).ConfigureAwait(false);
			}
		}

		public override async Task<List<bool>> ObjectsExists(IEnumerable<string> ids, CancellationToken cancellationToken = default) {
			AsyncFtpClient client = await GetClient().ConfigureAwait(false);

			List<bool> results = new List<bool>();
			foreach (string path in ids) {
				bool e = await client.FileExists(path).ConfigureAwait(false);
				results.Add(e);
			}

			return results;
		}

		public override async Task<bool> ObjectExists(string path, CancellationToken cancellationToken = default) {
			AsyncFtpClient client = await GetClient().ConfigureAwait(false);

			return await client.FileExists(path).ConfigureAwait(false);
		}

		public override async Task<List<StoreObject>> GetObjectsInfo(IEnumerable<string> ids, CancellationToken cancellationToken = default) {
			AsyncFtpClient client = await GetClient().ConfigureAwait(false);

			List<StoreObject> results = new List<StoreObject>();
			foreach (string path in ids) {
				string cpath = StoragePath.Normalize(path);
				string parentPath = StoragePath.GetParent(cpath);

				FtpListItem[] all = await client.GetListing(parentPath).ConfigureAwait(false);
				FtpListItem foundItem = all.FirstOrDefault(i => i.FullName == cpath);

				if (foundItem == null) {
					results.Add(null);
					continue;
				}

				StoreObject r = new StoreObject(path) {
					Size = foundItem.Size,
					DateModified = foundItem.Modified
				};
				results.Add(r);
			}
			return results;
		}

		public override async Task<Stream> OpenRead(string fullPath, CancellationToken cancellationToken = default) {
			AsyncFtpClient client = await GetClient().ConfigureAwait(false);

			try {
				return await client.OpenRead(fullPath, FtpDataType.Binary, 0, true).ConfigureAwait(false);
			}
			catch (FtpCommandException ex) when (ex.CompletionCode == "550") {
				return null;
			}
		}

		public override async Task SetObject(string fullPath, Stream dataStream, bool append, CancellationToken cancellationToken) {
			await SetObject(fullPath, dataStream, null, append, cancellationToken).ConfigureAwait(false);
		}
		public override async Task SetObject(string fullPath, Stream dataStream, string contentType, bool append = false, CancellationToken cancellationToken = default) {

			AsyncFtpClient client = await GetClient().ConfigureAwait(false);

			await retryPolicy.ExecuteAsync(async () => {
				string directory = Path.GetDirectoryName(fullPath);

				if (!string.IsNullOrWhiteSpace(directory) && !await client.DirectoryExists(directory).ConfigureAwait(false)) {
					await client.CreateDirectory(directory, cancellationToken);
				}

				using Stream dest = append
					? await client.OpenAppend(fullPath, FtpDataType.Binary, true, token: cancellationToken).ConfigureAwait(false)
					: await client.OpenWrite(fullPath, FtpDataType.Binary, true, token: cancellationToken).ConfigureAwait(false);

#if NETSTANDARD2_0
				await dataStream.CopyToAsync(dest).ConfigureAwait(false);
#else
				await dataStream.CopyToAsync(dest, cancellationToken).ConfigureAwait(false);
#endif
			}).ConfigureAwait(false);
		}

		public override void Dispose() {
			if (_dispose && !_client.IsDisposed) {
				_client.Dispose();
			}
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Gets information and capabilities of the connected FTP server.
		/// Returns a `Dictionary` with the following keys: `ServerOS`, `ServerType`, `SystemType`, `Capabilities`.
		/// </summary>
		public override async Task<Dictionary<string, object>> GetServer(CancellationToken cancellationToken = default) {

			AsyncFtpClient client = await GetClient().ConfigureAwait(false);

			return new Dictionary<string, object> {
				["ServerOS"] = client.ServerOS,
				["ServerType"] = client.ServerType,
				["SystemType"] = client.SystemType,
				["Capabilities"] = client.Capabilities,
			};
		}

		/// <summary>
		/// Creates a new folder on the FTP server.
		/// </summary>
		/// <param name="folderPath">Path to the new folder.</param>
		public override async Task CreateDirectory(string folderPath, bool force, CancellationToken cancellationToken = default) {

			AsyncFtpClient client = await GetClient().ConfigureAwait(false);

			await client.CreateDirectory(folderPath, force, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Deletes a folder on the FTP server.
		/// </summary>
		/// <param name="folderPath">Path to the folder.</param>
		/// <param name="recursive">Whether to delete all child files and folders.</param>
		public override async Task DeleteDirectory(string folderPath, bool recursive, CancellationToken cancellationToken = default) {

			AsyncFtpClient client = await GetClient().ConfigureAwait(false);

			await client.DeleteDirectory(folderPath, recursive ? FtpListOption.Recursive : FtpListOption.Auto, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Returns true if the specified directory exists on the FTP server.
		/// </summary>
		/// <param name="folderPath">Path to the directory.</param>
		/// <returns>
		public override async Task<bool> DirectoryExists(string folderPath, CancellationToken cancellationToken = default) {

			AsyncFtpClient client = await GetClient().ConfigureAwait(false);
			return await client.DirectoryExists(folderPath, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Moves a directory to a new location on the FTP server.
		/// </summary>
		/// <param name="sourceFolderPath">Source directory path.</param>
		/// <param name="destinationFolderPath">Destination directory path.</param>
		public override async Task MoveDirectory(string sourceFolderPath, string destinationFolderPath, CancellationToken cancellationToken = default) {

			AsyncFtpClient client = await GetClient().ConfigureAwait(false);
			await client.MoveDirectory(sourceFolderPath, destinationFolderPath, FtpRemoteExists.Overwrite, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Gets the Unix CHMOD permissions of a file on the FTP server.
		/// </summary>
		/// <param name="filePath">Path to the file.</param>
		/// <returns>
		/// The file permissions as a numeric CHMOD value (for example, 644 or 755).
		/// </returns>
		public override async Task<int> GetFilePermissions(string filePath, CancellationToken cancellationToken = default) {

			AsyncFtpClient client = await GetClient().ConfigureAwait(false);

			FtpListItem item = await client.GetFilePermissions(filePath, cancellationToken).ConfigureAwait(false);
			return item?.Chmod ?? 0;
		}

		/// <summary>
		/// Sets the Unix CHMOD permissions of a file on the FTP server.
		/// </summary>
		/// <param name="filePath">Path to the file.</param>
		/// <param name="permissions">Permissions as a numeric CHMOD value (for example, 644 or 755).</param>
		public override async Task SetFilePermissions(string filePath, int permissions, CancellationToken cancellationToken = default) {

			AsyncFtpClient client = await GetClient().ConfigureAwait(false);
			await client.Chmod(filePath, permissions, cancellationToken).ConfigureAwait(false);
		}

	}
}
