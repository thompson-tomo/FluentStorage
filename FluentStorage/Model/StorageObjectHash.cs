using FluentStorage.Enums;
using FluentStorage.Utils.Extensions;
using FluentStorage.Utils.Hashing;
using System;
using System.IO;

namespace FluentStorage.Model {

	/// <summary>
	/// Represents a computed hash of an object.
	/// </summary>
	public sealed class StorageObjectHash {

		/// <summary>
		/// Gets the hash algorithm.
		/// </summary>
		public StorageHash Algorithm { get; internal set; }

		/// <summary>
		/// Gets the full path for the object which this hash belongs.
		/// </summary>
		public string FullPath { get; internal set; }

		/// <summary>
		/// Gets the computed hash value.
		/// </summary>
		public string Value { get; internal set; }

		/// <summary>
		/// Gets whether this object represents a valid hash.
		/// </summary>
		public bool IsValid => !string.IsNullOrEmpty(Value);

		/// <summary>
		/// Basic constructor used internally.
		/// </summary>
		public StorageObjectHash(string fullPath, string hashValue, StorageHash algo) {
			Algorithm = algo;
			FullPath = fullPath;
			Value = hashValue;
		}

		/// <summary>
		/// Computes the hash for the specified object and compares it
		/// to this hash value.
		/// </summary>
		/// <param name="objectPath">The object to verify.</param>
		/// <returns>True if the computed hash matches.</returns>
		public bool Verify(string objectPath) {
			using (var stream = File.OpenRead(objectPath)) {
				return Verify(stream);
			}
		}

		/// <summary>
		/// Computes the hash for the specified stream and compares it to this hash value.
		/// </summary>
		/// <param name="stream">The stream to verify.</param>
		/// <returns>True if the computed hash matches.</returns>
		public bool Verify(Stream stream) {
			if (!IsValid) {
				return false;
			}

			string hash = HashUtility.HashStream(stream, Algorithm).ToHexString();

			if (hash.Equals(Value, StringComparison.OrdinalIgnoreCase)) {
				return true;
			}

			// Some CRC implementations include a leading zero.
			if (Algorithm == StorageHash.CRC32 &&
				hash.TrimStart('0').Equals(Value.TrimStart('0'), StringComparison.OrdinalIgnoreCase)) {
				return true;
			}

			return false;
		}

	}
}