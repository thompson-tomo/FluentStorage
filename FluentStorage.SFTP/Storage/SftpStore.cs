using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using FluentStorage.Storage;
using FluentStorage.Enums;
using FluentStorage.Model;

namespace FluentStorage.SFTP {
	/// <summary>
	/// Manages a single connected SFTP server using SSH.NET. Exclusively synchronous.
	/// </summary>
	public class SftpStore : StoreBase {
		/// <summary>
		/// The retry policy
		/// </summary>
		private static readonly AsyncRetryPolicy _retryPolicy = Policy.Handle<Exception>().RetryAsync(3);

		/// <summary>
		/// Holds a reference to the <see cref="T:FluentStorage.SFTP.SshNetSftpBlobStorage" /> instance.
		/// </summary>
		private readonly SftpClient _client;

		/// <summary>
		/// A boolean flag indicating whether to dispose the client instance upon disposing this object.
		/// </summary>
		private readonly bool _disposeClient;

		/// <summary>
		/// A boolean flag indicating whether this instance is disposed.
		/// </summary>
		private bool _disposed = false;

		/// <summary>
		/// Object used in in ListDirectoryAsync to avoid accessing collections from multiple threads at the same time.
		/// </summary>
		private readonly object _listDirectoryLockObject = new object();

		/// <summary>
		/// Gets or sets the maximum retry count.
		/// </summary>
		/// <value>
		/// The maximum retry count.
		/// </value>
		public int MaxRetryCount { get; set; } = 3;

		/// <summary>
		/// Root directory, relative to which all paths will resolve to.
		/// </summary>
		/// <value>
		/// Directory required or null.
		/// </value>
		public string RootDirectory { get; private set; }

		/// <summary>
		/// Enable/disable calling SetLength() on created SftpStream when writing new blobs. Default true (set length).
		/// Not required in all implementations. Requires sftp user permissions on file attributes.
		/// </summary>
		public bool SetLengthOnNewStream { get; set; } = true;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FluentStorage.SFTP.SshNetSftpBlobStorage" /> class.
		/// </summary>
		/// <param name="connectionInfo">The connection info.</param>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="connectionInfo" /> is <b>null</b>.</exception>
		public SftpStore(ConnectionInfo connectionInfo)
		  : this(new SftpClient(connectionInfo), true) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FluentStorage.SFTP.SshNetSftpBlobStorage" /> class.
		/// </summary>
		/// <param name="host">Connection host.</param>
		/// <param name="port">Connection port.</param>
		/// <param name="username">Authentication username.</param>
		/// <param name="password">Authentication password.</param>
		/// <param name="path">Starting root directory or null.</param>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="password" /> is <b>null</b>.</exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="host" /> is invalid. <para>-or-</para> <paramref name="username" /> is <b>null</b> or contains only whitespace characters.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="port" /> is not within <see cref="F:System.Net.IPEndPoint.MinPort" /> and <see cref="F:System.Net.IPEndPoint.MaxPort" />.</exception>
		public SftpStore(string host, int port, string username, string password, string path)
		  : this(new SftpClient(host, port, username, password), true) {
			RootDirectory = path;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FluentStorage.SFTP.SshNetSftpBlobStorage" /> class.
		/// </summary>
		/// <param name="host">Connection host.</param>
		/// <param name="username">Authentication username.</param>
		/// <param name="password">Authentication password.</param>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="password" /> is <b>null</b>.</exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="host" /> is invalid. <para>-or-</para> <paramref name="username" /> is <b>null</b> contains only whitespace characters.</exception>
		public SftpStore(string host, string username, string password)
		  : this(new SftpClient(host, username, password), true) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FluentStorage.SFTP.SshNetSftpBlobStorage" /> class.
		/// </summary>
		/// <param name="host">Connection host.</param>
		/// <param name="port">Connection port.</param>
		/// <param name="username">Authentication username.</param>
		/// <param name="keyFiles">Authentication private key file(s) .</param>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="keyFiles" /> is <b>null</b>.</exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="host" /> is invalid. <para>-or-</para> <paramref name="username" /> is nu<b>null</b>ll or contains only whitespace characters.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="port" /> is not within <see cref="F:System.Net.IPEndPoint.MinPort" /> and <see cref="F:System.Net.IPEndPoint.MaxPort" />.</exception>
		public SftpStore(string host, int port, string username, params PrivateKeyFile[] keyFiles)
		  : this(new SftpClient(host, port, username, keyFiles), true) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FluentStorage.SFTP.SshNetSftpBlobStorage" /> class.
		/// </summary>
		/// <param name="host">Connection host.</param>
		/// <param name="username">Authentication username.</param>
		/// <param name="keyFiles">Authentication private key file(s) .</param>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="keyFiles" /> is <b>null</b>.</exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="host" /> is invalid. <para>-or-</para> <paramref name="username" /> is <b>null</b> or contains only whitespace characters.</exception>
		public SftpStore(string host, string username, params PrivateKeyFile[] keyFiles)
		  : this(new SftpClient(host, username, keyFiles), true) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FluentStorage.SFTP.SshNetSftpBlobStorage" /> class.
		/// </summary>
		/// <param name="sftpClient">The SFTP client.</param>
		/// <param name="disposeClient">if set to <see langword="true" /> [dispose client].</param>
		/// <exception cref="System.ArgumentNullException">sftpClient</exception>
		public SftpStore(SftpClient sftpClient, bool disposeClient = false) {
			_client = sftpClient ?? throw new ArgumentNullException(nameof(sftpClient));
			_client.HostKeyReceived += (sender, args) => { };
			_disposeClient = disposeClient;
		}

		public override bool IsFileSystem() {
			return true;
		}

		/// <summary>
		/// Deletes a list of objects by their full path.
		/// </summary>
		/// <param name="fullPaths">The collection of full paths to delete. If this paths points to a folder, the folder is deleted recursively.</param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public override async Task DeleteObjects(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			await Task.WhenAll(fullPaths.Select(fullPath => DeleteObject(fullPath, cancellationToken))).ConfigureAwait(false);
		}

		/// <summary>
		/// Deletes an object by its full path.
		/// </summary>
		/// <param name="fullPath">The full path.</param>
		/// <param name="client">The sftp client to use.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns></returns>
		public override async Task DeleteObject(string fullPath, CancellationToken cancellationToken = default) {
			if (cancellationToken.IsCancellationRequested) {
				return;
			}

			SftpClient client = Client();

			fullPath = StoragePath.Combine(RootDirectory, StoragePath.Normalize(fullPath));

			client.Delete(fullPath);
		}

		/// <summary>
		/// Determine whether the blobs exists in the storage
		/// </summary>
		/// <param name="fullPaths">List of paths to blobs</param>
		/// <param name="cancellationToken"></param>
		/// <returns>
		/// List of results of true and false indicating existence
		/// </returns>
		public override async Task<List<bool>> ObjectsExists(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			return (await Task.WhenAll(fullPaths.Select(fullPath => ObjectExists(fullPath, cancellationToken))).ConfigureAwait(false)).ToList();
		}

		/// <summary>
		/// Determine whether the blobs exists in the storage
		/// </summary>
		/// <param name="fullPath">List of paths to blobs</param>
		/// <param name="cancellationToken"></param>
		/// <returns>
		/// List of results of true and false indicating existence
		/// </returns>
		public override async Task<bool> ObjectExists(string fullPath, CancellationToken cancellationToken = default) {

			if (cancellationToken.IsCancellationRequested) {
				return false;
			}

			SftpClient client = Client();

			fullPath = StoragePath.Combine(RootDirectory, StoragePath.Normalize(fullPath));

			bool fullPathExists = client.Exists(fullPath);

			return fullPathExists;
		}

		public override async Task<StoreObject> GetObjectInfo(string path, CancellationToken cancellationToken = default) {
			return (await GetObjectsInfo(new List<string> { path }, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
		}
		public override async Task<List<StoreObject>> GetObjectsInfo(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			SftpClient client = Client();

			var results = new List<StoreObject>();
			var fullPathsWithRoot = fullPaths.Select(fullPath => StoragePath.Combine(RootDirectory, fullPath));
			foreach (IGrouping<string, string> fullPathGrouping in fullPathsWithRoot.GroupBy(StoragePath.GetParent)) {
				string fullPath = fullPathGrouping.SingleOrDefault();

				if (cancellationToken.IsCancellationRequested) {
					break;
				}

				try {
					List<StoreObject> blobCollection = new List<StoreObject>();

					await foreach (SftpFile sftpFile in client.ListDirectoryAsync(fullPathGrouping.Key, cancellationToken)) {
						if ((sftpFile.IsDirectory || sftpFile.IsRegularFile) && sftpFile.FullName == fullPath) {
							blobCollection.Add(ConvertSftpFileToBlob(sftpFile));
						}
					}

					if (blobCollection.Any()) {
						// If using a RoodDirectory, remove from full path.
						if (RootDirectory != null) {
							foreach (var b in blobCollection) {
								b.SetFullPath(b.FullPath.Substring(RootDirectory.Length + 1));
							}
						}
						results.AddRange(blobCollection);
					}
					else {
						results.Add(null);
					}
				}
				catch (Renci.SshNet.Common.SftpPathNotFoundException) {
					// If the directoy did not exists, the SSH client will return this exception. To
					// normalize with other storage implementations, we'll return null without
					// raising an error.
					results.Add(null);
				}
			}

			return results;
		}

		/// <summary>
		/// Returns the list of available blobs
		/// </summary>
		/// <param name="options"></param>
		/// <param name="cancellationToken"></param>
		/// <returns>
		/// List of blob IDs
		/// </returns>
		public override async Task<List<StoreObject>> ListObjects(StorageListOptions options = null, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			options ??= new StorageListOptions();
			options.MaxResults ??= int.MaxValue;
			options.BrowseFilter ??= _ => true;

			SftpClient client = Client();

			var folder = StoragePath.Combine(RootDirectory, StoragePath.Normalize(options.FolderPath));

			var blobCollection = await ListDirectoryAsync(client, folder, options, cancellationToken);

			if (RootDirectory != null) {
				foreach (var b in blobCollection) {
					b.SetFullPath(b.FullPath.Substring(RootDirectory.Length + 1));
				}
			}

			return blobCollection;
		}


		/// <summary>
		/// Used internally. Returns a list of available blobs. 
		/// </summary>
		/// <param name="client"></param>
		/// <param name="folderToList"></param>
		/// <param name="options"></param>
		/// <param name="cancellationToken"></param>
		/// <returns>List of blob IDs</returns>
		async Task<List<StoreObject>> ListDirectoryAsync(SftpClient client, string folderToList, StorageListOptions options, CancellationToken cancellationToken = default) {

			List<StoreObject> blobCollection = new List<StoreObject>();

			// Note: options.FolderPath is not used here, we use the folderToList which is passed in.
			List<SftpFile> directoryContents = new List<SftpFile>();
			await foreach (SftpFile sftpFile in client.ListDirectoryAsync(folderToList, cancellationToken)) {
				if ((options.FilePrefix == null || sftpFile.Name.StartsWith(options.FilePrefix))
							 && (sftpFile.IsDirectory || sftpFile.IsRegularFile || sftpFile.OwnerCanRead)
							 && !cancellationToken.IsCancellationRequested
							 && sftpFile.Name != "."
							 && sftpFile.Name != "..") {
					directoryContents.Add(sftpFile);
				}
			}

			var tempBlobCollection = directoryContents
				.Take(options.MaxResults.Value)
				.Select(ConvertSftpFileToBlob)
				.Where(options.BrowseFilter).ToList();

			blobCollection.AddRange(tempBlobCollection);

			if (options.Recurse == true) {
				IEnumerable<string> subFoldersToList = tempBlobCollection
					.Where(x => x.IsFolder == true)
					.Select(x => x.FullPath);

#if NET6_0_OR_GREATER
				await Parallel.ForEachAsync(subFoldersToList, async (subFolder, token) => {
					var tempForEachBlobCollection = await ListDirectoryAsync(client, subFolder, options, cancellationToken);
					lock (_listDirectoryLockObject) {
						blobCollection.AddRange(tempForEachBlobCollection);
					}
				});
#else
				foreach (string subFolder in subFoldersToList) {
					var tempForEachBlobCollection = await ListDirectoryAsync(client, subFolder, options, cancellationToken);
					blobCollection.AddRange(tempForEachBlobCollection);
				}
#endif
			}

			return blobCollection;
		}

		/// <summary>
		/// Opens a file for reading and returns its content stream.
		/// It is your responsibility to close and dispose this stream after use.
		/// </summary>
		public override async Task<Stream> OpenRead(string fullPath, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			fullPath = StoragePath.Combine(RootDirectory, StoragePath.Normalize(fullPath));

			SftpClient client = Client();

			MemoryStream stream = new MemoryStream();

			try {
				await Task.Run(() => Policy.Handle<Exception>().Retry(MaxRetryCount).Execute(() => client.DownloadFile(fullPath, stream)));
				stream.Position = 0;
				return stream;
			}
			catch (Exception /*exception*/) {
				stream?.Dispose();
				return null;
			}
		}

		public override async Task<Stream> OpenRange(string fullPath,long offset,long length, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			fullPath = StoragePath.Combine(RootDirectory, StoragePath.Normalize(fullPath));

			SftpClient client = Client();

			Stream stream = client.OpenRead(fullPath);
			stream.Seek(offset, SeekOrigin.Begin);

			return stream;
		}

		public override bool IsSeekable() {
			return true;
		}
		public override async Task<long> GetObjectLength(string fullPath, long defaultValue = -1, CancellationToken cancellationToken = default) {
			try {
				ThrowIfDisposed();

				fullPath = StoragePath.Combine(RootDirectory, StoragePath.Normalize(fullPath));

				SftpClient client = Client();

				var attrib = client.GetAttributes(fullPath);

				return attrib != null ? attrib.Size : defaultValue;
			}
			catch {
				return defaultValue;
			}
		}

		/// <summary>
		/// Rename a file on the SFTP server.
		/// </summary>
		/// <param name="oldPath">Existing Remote file path</param>
		/// <param name="newPath">New Remote file path</param>
		public override async Task<bool> MoveObject(string oldPath, string newPath, bool overwrite, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			oldPath = StoragePath.Combine(RootDirectory, StoragePath.Normalize(oldPath));
			newPath = StoragePath.Combine(RootDirectory, StoragePath.Normalize(newPath));

			SftpClient client = Client();

			if (!overwrite && await ObjectExists(newPath)) return false;

			client.RenameFile(oldPath, newPath);

			return true;
		}

		public override async Task SetObject(string fullPath, Stream dataStream, bool append, CancellationToken cancellationToken = default) {
			await SetObject(fullPath, dataStream, null, append, cancellationToken).ConfigureAwait(false);
		}
		/// <summary>
		/// Uploads data to a file.
		/// </summary>
		/// <param name="fullPath">Remote file path</param>
		/// <param name="dataStream">Stream to upload from</param>
		/// <param name="append">When true, appends to the file instead of writing a new one.</param>
		/// <param name="cancellationToken"></param>
		/// <returns>
		/// Writeable stream
		/// </returns>
		public override async Task SetObject(string fullPath, Stream dataStream, string contentType, bool append = false, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			SftpClient client = Client();
			var fullPathWithRoot = StoragePath.Combine(RootDirectory, StoragePath.Normalize(fullPath));
			var fileMode = append ? FileMode.Append : FileMode.OpenOrCreate;

			// First, for speed, let's try to write the file assuming the directory requested already exists.

			try {
				using (Stream dest = client.Open(fullPathWithRoot, fileMode, FileAccess.Write)) {
					await dataStream.CopyToAsync(dest).ConfigureAwait(false);
					if (append == false && SetLengthOnNewStream) {
						dest.SetLength(dataStream.Length);
					}
				}
				return;
			}
			catch (Renci.SshNet.Common.SftpPathNotFoundException) {
				// If the folder did not exist, continue below.
			}

			// Create any non-existing directories. We'll need to recursively check each part and
			// create if it does not exist.
			var parts = StoragePath.Split(fullPath).ToList();
			parts.RemoveAt(parts.Count - 1);

			await _retryPolicy.ExecuteAsync(async () => {
				var fullFolder = RootDirectory;
				foreach (var folder in parts) {
					fullFolder = StoragePath.Combine(fullFolder, folder);
					if (!client.Exists(fullFolder))
						client.CreateDirectory(fullFolder);
				}

				using (Stream dest = client.Open(fullPathWithRoot, fileMode, FileAccess.Write)) {
					await dataStream.CopyToAsync(dest).ConfigureAwait(false);
					if (append == false && SetLengthOnNewStream) {
						dest.SetLength(dataStream.Length);
					}
				}
			}).ConfigureAwait(false);
		}

		/// <summary>
		/// Returns the SftpClient instance for this store.
		/// </summary>
		public override async Task<object> GetClient() {
			return Client();
		}

		private SftpClient Client() {
			ThrowIfDisposed();

			if (!_client.IsConnected) {
				_client.Connect();
			}

			return _client;
		}

		/// <summary>
		/// Converts the specified <see cref="T:Renci.SshNet.Sftp.SftpFile"/> into a <see cref="T:FluentStorage.Blobs.Blob"/> instance.
		/// </summary>
		/// <param name="file">The file.</param>
		/// <returns></returns>
		private static StoreObject ConvertSftpFileToBlob(SftpFile file) {
			if (file.IsDirectory || file.IsRegularFile || file.OwnerCanRead) {
				StorageObjectType itemKind = file.IsDirectory
				   ? StorageObjectType.Folder
				   : StorageObjectType.File;

				return new StoreObject(file.FullName, itemKind) {
					Size = file.Length,
					DateModified = file.LastWriteTime
				};
			}

			return null;
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public override void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources.
		/// </summary>
		/// <param name="disposing"><see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposing) {
			if (_disposed) {
				return;
			}

			// Release any managed resources here.
			if (disposing && _disposeClient) {
				_client.Dispose();
			}

			_disposed = true;
		}

		/// <summary>
		/// Throws an <see cref="T:System.ObjectDisposedException" /> if this object has been disposed.
		/// </summary>
		/// <exception cref="T:System.ObjectDisposedException">The current instance is disposed.</exception>
		protected void ThrowIfDisposed() {
			if (_disposed) {
				throw new ObjectDisposedException(GetType().FullName);
			}
		}

		/// <summary>
		/// Gets information about the connected SFTP server.
		///
		/// Returns a dictionary with `ProtocolVersion`, `ServerVersion`, `ClientVersion`,
		/// `ClientEncryption`, `ServerEncryption`, `KeyExchangeAlgorithm`,
		/// `ClientHmacAlgorithm`, `ServerHmacAlgorithm`,
		/// `ClientCompressionAlgorithm`, `ServerCompressionAlgorithm`.
		/// </summary>
		public override async Task<Dictionary<string, object>> GetServer(CancellationToken cancellationToken = default) {

			SftpClient client = Client();

			return new Dictionary<string, object> {

				// SFTP
				["ProtocolVersion"] = client.ProtocolVersion,

				// Server
				["ServerVersion"] = client.ConnectionInfo.ServerVersion,
				["ClientVersion"] = client.ConnectionInfo.ClientVersion,

				// Encryption
				["ClientEncryption"] = client.ConnectionInfo.CurrentClientEncryption,
				["ServerEncryption"] = client.ConnectionInfo.CurrentServerEncryption,
				["KeyExchangeAlgorithm"] = client.ConnectionInfo.CurrentKeyExchangeAlgorithm,

				// HMAC / Integrity
				["ClientHmacAlgorithm"] = client.ConnectionInfo.CurrentClientHmacAlgorithm,
				["ServerHmacAlgorithm"] = client.ConnectionInfo.CurrentServerHmacAlgorithm,

				// Compression
				["ClientCompressionAlgorithm"] = client.ConnectionInfo.CurrentClientCompressionAlgorithm,
				["ServerCompressionAlgorithm"] = client.ConnectionInfo.CurrentServerCompressionAlgorithm,
			};
		}

		/// <summary>
		/// Creates a new folder on the SFTP server.
		/// </summary>
		/// <param name="folderPath">Path to the new folder.</param>
		public override async Task CreateDirectory(string folderPath, bool force, CancellationToken cancellationToken = default) {

			SftpClient client = Client();

			folderPath = StoragePath.Combine(RootDirectory, StoragePath.Normalize(folderPath));

			client.CreateDirectory(folderPath);
		}

		/// <summary>
		/// Deletes a folder.
		/// </summary>
		/// <param name="folderPath">Path to the folder.</param>
		/// <param name="recursive">Whether to delete all child files and folders.</param>
		public override async Task DeleteDirectory(string folderPath, bool recursive, CancellationToken cancellationToken = default) {

			SftpClient client = Client();

			folderPath = StoragePath.Combine(RootDirectory, StoragePath.Normalize(folderPath));

			if (await DirectoryExists(folderPath, cancellationToken)) {
				if (recursive) {
					DeleteDirectoryRecursive(client, folderPath);
				}
				else {
					client.DeleteDirectory(folderPath);
				}
			}
		}

		private static void DeleteDirectoryRecursive(SftpClient client, string folderPath) {

			foreach (var entry in client.ListDirectory(folderPath)) {

				if (entry.Name == "." || entry.Name == "..")
					continue;

				if (entry.IsDirectory) {
					DeleteDirectoryRecursive(client, entry.FullName);
				}
				else {
					client.DeleteFile(entry.FullName);
				}
			}

			client.DeleteDirectory(folderPath);
		}

		/// <summary>
		/// Determines whether the specified directory exists on the SFTP server.
		/// </summary>
		/// <param name="folderPath">Path to the directory.</param>
		/// <returns>
		/// <c>true</c> if the directory exists; otherwise, <c>false</c>.
		/// </returns>
		public override async Task<bool> DirectoryExists(string folderPath, CancellationToken cancellationToken = default) {

			SftpClient client = Client();

			folderPath = StoragePath.Combine(RootDirectory, StoragePath.Normalize(folderPath));

			return client.Exists(folderPath) && client.GetAttributes(folderPath).IsDirectory;
		}

		/// <summary>
		/// Moves a directory to a new location on the SFTP server.
		/// </summary>
		/// <param name="sourceFolderPath">Source directory path.</param>
		/// <param name="destinationFolderPath">Destination directory path.</param>
		public override async Task MoveDirectory(string sourceFolderPath, string destinationFolderPath, CancellationToken cancellationToken = default) {

			SftpClient client = Client();

			sourceFolderPath = StoragePath.Combine(RootDirectory, StoragePath.Normalize(sourceFolderPath));
			destinationFolderPath = StoragePath.Combine(RootDirectory, StoragePath.Normalize(destinationFolderPath));

			client.RenameFile(sourceFolderPath, destinationFolderPath);
		}

		/// <summary>
		/// Gets the Unix CHMOD permissions of a file on the SFTP server.
		/// </summary>
		/// <param name="filePath">Path to the file.</param>
		/// <returns>
		/// The file permissions as a numeric CHMOD value (for example, 644 or 755).
		/// </returns>
		public override async Task<int> GetFilePermissions(string filePath, CancellationToken cancellationToken = default) {

			SftpClient client = Client();

			filePath = StoragePath.Combine(RootDirectory, StoragePath.Normalize(filePath));

			var attributes = client.GetAttributes(filePath);

			// Convert the permission flags to a traditional octal CHMOD value.
			int chmod =
				((attributes.OwnerCanRead ? 4 : 0) + (attributes.OwnerCanWrite ? 2 : 0) + (attributes.OwnerCanExecute ? 1 : 0)) * 100 +
				((attributes.GroupCanRead ? 4 : 0) + (attributes.GroupCanWrite ? 2 : 0) + (attributes.GroupCanExecute ? 1 : 0)) * 10 +
				((attributes.OthersCanRead ? 4 : 0) + (attributes.OthersCanWrite ? 2 : 0) + (attributes.OthersCanExecute ? 1 : 0));

			return chmod;
		}

		/// <summary>
		/// Sets the Unix CHMOD permissions of a file on the SFTP server.
		/// </summary>
		/// <param name="filePath">Path to the file.</param>
		/// <param name="permissions">Permissions as a numeric CHMOD value (for example, 644 or 755).</param>
		public override async Task SetFilePermissions(string filePath, int permissions, CancellationToken cancellationToken = default) {

			SftpClient client = Client();

			filePath = StoragePath.Combine(RootDirectory, StoragePath.Normalize(filePath));

			client.ChangePermissions(filePath, (short)permissions);
		}

		/// <summary>
		/// Downloads a file from the SFTP server.
		/// </summary>
		/// <param name="fullPath">Remote file path.</param>
		/// <param name="filePath">Destination path of the local file.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		public override async Task DownloadObject(string fullPath, string filePath, bool overwrite, CancellationToken cancellationToken = default) {

			// skip if overwriting disabled and local file exists
			if (!overwrite && File.Exists(fullPath)) return;

			SftpClient client = Client();

			fullPath = StoragePath.Combine(RootDirectory, StoragePath.Normalize(fullPath));

			client.DownloadFile(fullPath, File.Create(filePath));
		}

		/// <summary>
		/// Uploads a local file to the SFTP server.
		/// </summary>
		/// <param name="fullPath">Remote file path.</param>
		/// <param name="filePath">Local file path.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		public override async Task UploadObject(string fullPath, string filePath, bool overwrite, CancellationToken cancellationToken = default) {

			// exit if local file doesnt exist
			if (!File.Exists(fullPath)) return;

			SftpClient client = Client();

			fullPath = StoragePath.Combine(RootDirectory, StoragePath.Normalize(fullPath));

			using FileStream stream = File.OpenRead(filePath);

			client.UploadFile(stream, fullPath, overwrite);
		}

		/// <summary>
		/// Downloads a file from the SFTP server into a byte array.
		/// </summary>
		/// <param name="fullPath">Remote file path.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>The contents of the object.</returns>
		public override async Task<byte[]> GetBytes(string fullPath, CancellationToken cancellationToken = default) {

			SftpClient client = Client();

			fullPath = StoragePath.Combine(RootDirectory, StoragePath.Normalize(fullPath));

			using MemoryStream stream = new();

			client.DownloadFile(fullPath, stream);

			return stream.ToArray();
		}

		/// <summary>
		/// Uploads a file byte array to the SFTP server.
		/// </summary>
		/// <param name="fullPath">Remote file path.</param>
		/// <param name="data">Data to write.</param>
		/// <param name="append">
		/// <c>true</c> to append to the existing object; otherwise, overwrites the object.
		/// </param>
		/// <param name="cancellationToken">Cancellation token.</param>
		public override async Task SetBytes(string fullPath, byte[] data, bool append = false, CancellationToken cancellationToken = default) {

			// exit if invalid data
			if (data == null || data.Length == 0) return;

			SftpClient client = Client();

			fullPath = StoragePath.Combine(RootDirectory, StoragePath.Normalize(fullPath));

			using Stream stream = append
				? client.Open(fullPath, FileMode.Append, FileAccess.Write)
				: client.Open(fullPath, FileMode.Create, FileAccess.Write);

			await stream.WriteAsync(data, 0, data.Length, cancellationToken);
		}

	}
}