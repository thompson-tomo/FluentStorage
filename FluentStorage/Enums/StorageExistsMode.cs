using System;
using System.Collections.Generic;
using System.Text;

namespace FluentStorage.Enums {
	/// <summary>
	/// Determines what to do when the destination file/object already exists.
	/// </summary>
	public enum StorageExistsMode {
		/// <summary>
		/// Throw an exception if the destination already exists.
		/// </summary>
		Throw,

		/// <summary>
		/// Skip the transfer if the destination already exists.
		/// </summary>
		Skip,

		/// <summary>
		/// Replace the destination if it already exists.
		/// </summary>
		Overwrite,
	}
}
