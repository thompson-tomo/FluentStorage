using FluentStorage.Storage;

namespace FluentStorage {
	/// <summary>
	/// Virtual storage
	/// </summary>
	public interface IVirtualStorage : IBucket {
		/// <summary>
		/// Mounts a storage to virtual path
		/// </summary>
		void Mount(string path, IBucket storage);
	}
}
