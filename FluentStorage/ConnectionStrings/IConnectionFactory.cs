using FluentStorage.Storage;
using FluentStorage.Queue;

namespace FluentStorage.ConnectionStrings {
	/// <summary>
	/// Connection factory is responsible for creating storage instances from connection strings. It
	/// is usually implemented by every external module, however is optional.
	/// </summary>
	public interface IConnectionFactory {
		/// <summary>
		/// Creates a blob storage instance from connection string if possible. When this factory does not support this connection
		/// string it returns null.
		/// </summary>
		IStore CreateStore(ConnectionString connectionString);

		/// <summary>
		/// Creates a message publisher
		/// </summary>
		IQueue CreateQueue(ConnectionString connectionString);
	}
}
