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
		/// Computes the hash for the specified local file and compares it to this hash value.
		/// </summary>
		/// <param name="localFilePath">The local file path to verify.</param>
		/// <returns>True if the computed hash matches.</returns>
		public bool Verify(string localFilePath) {
			var bytes = File.ReadAllBytes(localFilePath);
			return Verify(bytes);
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
			return VerifyHash(HashUtility.HashStream(stream, Algorithm));
		}

		/// <summary>
		/// Computes the hash for the specified byte array and compares it to this hash value.
		/// </summary>
		/// <param name="bytes">The stream to verify.</param>
		/// <returns>True if the computed hash matches.</returns>
		public bool Verify(byte[] bytes) {
			if (!IsValid) {
				return false;
			}
			return VerifyHash(HashUtility.HashBytes(bytes, Algorithm));
		}

		private bool VerifyHash(string hash) {
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