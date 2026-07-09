using FluentStorage.Storage;
using FluentStorage.Storage.Files;
using FluentStorage.ConnectionString;
using FluentStorage.Queue;
using System;

namespace FluentStorage {
	/// <summary>
	/// Helper syntax for creating instances of storage library objects
	/// </summary>
	public static class StorageFactory {

		/// <summary>
		/// Call to initialise a module
		/// </summary>
		public static void Use(IExternalModule module) {
			if (module == null) {
				throw new ArgumentNullException(nameof(module));
			}

			IConnectionFactory connectionFactory = module.ConnectionFactory;
			if (connectionFactory != null) {
				ConnectionStringFactory.Register(connectionFactory);
			}

		}

		/// <summary>
		/// Creates a blob storage instance from a connection string
		/// </summary>
		public static IBucket FromConnectionString(string connectionString) {
			return ConnectionStringFactory.CreateBlobStorage(connectionString);
		}

		/// <summary>
		/// Creates an instance in a specific disk directory
		/// <param name="directoryFullName">Root directory</param>
		/// </summary>
		public static IBucket DirectoryFiles(string directoryFullName) {
			return new DiskDirectoryBlobStorage(directoryFullName);
		}

		/// <summary>
		/// Zip file
		/// </summary>
		public static IBucket ZipFile(string filePath) {
			return new ZipFileBlobStorage(filePath);
		}

		/// <summary>
		/// Creates an instance of blob storage which stores everyting in memory. Useful for testing purposes only or if blobs don't
		/// take much space.
		/// </summary>
		/// <returns>In-memory blob storage instance</returns>
		public static IBucket InMemory() {
			return new InMemoryBlobStorage();
		}

		/// <summary>
		/// Creates a virtual storage where you can mount other storage providers to a specific virtual directory
		/// </summary>
		public static IVirtualStorage Virtual() {
			return new VirtualStorage();
		}

	}

}