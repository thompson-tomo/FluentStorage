using System.Threading;
using System.Threading.Tasks;

namespace FluentStorage.Storage {
	/// <summary>
	/// Object Storage that supports file systems such as local disk, FTP and SFTP.
	/// </summary>
	public interface IFileSystem {
		/// <summary>
		/// Rename a blob (folder or file)
		/// </summary>
		/// <param name="oldPath"></param>
		/// <param name="newPath"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		Task RenameAsync(string oldPath, string newPath, CancellationToken cancellationToken = default);

		/// <summary>
		/// Creates a new folder
		/// </summary>
		/// <param name="folderPath">Path to the new folder.</param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		Task CreateFolderAsync(string folderPath, CancellationToken cancellationToken = default);

	}
}
