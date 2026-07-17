using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FluentStorage {
	/// <summary>
	/// A simple unified path system designed to work across all storage providers, without having to fiddle with path intracacies.
	/// Suitable for all cloud storage like S3, S3-compatible, Azure Blob, GCP, R2, MinIO and more.
	/// Suitable for file systems like local disk, FTP, SFTP.
	/// </summary>
	public static class StoragePath {
		/// <summary>
		/// Character used to split paths 
		/// </summary>
		public const char PathSeparator = '/';

		/// <summary>
		/// Character used to split paths as a string value
		/// </summary>
		public static readonly string PathSeparatorString = new string(PathSeparator, 1);

		/// <summary>
		/// Folder name for leveling up the path
		/// </summary>
		public static readonly string LevelUpFolderName = "..";
		/// <summary>
		/// Combines path parts using the storage path separator.
		/// Null and empty parts are ignored.
		/// </summary>
		public static string Combine(IEnumerable<string> parts) {
			if (parts == null)
				return string.Empty;

			var sb = new StringBuilder();
			bool first = true;

			foreach (string part in parts) {
				if (string.IsNullOrEmpty(part))
					continue;

				string normalized = NormalizePart(part);
				if (normalized.Length == 0)
					continue;

				if (!first)
					sb.Append(PathSeparator);

				sb.Append(normalized);
				first = false;
			}

			return sb.ToString();
		}

		/// <summary>
		/// Splits a path into normalized parts.
		/// </summary>
		public static string[] Split(string path) {
			if (path == null)
				return null;

			path = Normalize(path);

			return path.Length == 0
				? Array.Empty<string>()
				: path.Split(PathSeparator);
		}

		/// <summary>
		/// Gets the parent path.
		/// </summary>
		public static string GetParent(string path) {
			if (path == null)
				return null;

			path = Normalize(path);

			if (path.Length == 0)
				return null;

			int last = path.LastIndexOf(PathSeparator);

			return last < 0
				? string.Empty
				: path.Substring(0, last);
		}

		/// <summary>
		/// Combines parts of path
		/// </summary>
		/// <param name="parts"></param>
		/// <returns></returns>
		public static string Combine(params string[] parts) {
			return Combine((IEnumerable<string>)parts);
		}

		/// <summary>
		/// Normalizes any file or object path into a simple unified path system.
		/// Suitable for all cloud storage like S3, S3-compatible, Azure Blob, GCP, R2, MinIO and more.
		/// Suitable for file systems like local disk, FTP, SFTP.
		///
		/// Rules:
		/// 1. Uses '/' as the path separator.
		/// 2. Converts '\' to '/'.
		/// 3. Removes any leading and trailing separator.
		/// 4. Collapses duplicate separators.
		/// 5. Preserves '.' and '..' path segments.
		/// 6. Returns an empty string for null, empty or root paths.
		/// </summary>
		public static string Normalize(string path) {

			/*
			- null                     -> ""
			- ""                       -> ""
			- "/"                      -> ""
			- "folder"                 -> "folder"
			- "/folder/"               -> "folder"
			- "\\folder\\file.txt"     -> "folder/file.txt"
			- "folder//sub///file.txt" -> "folder/sub/file.txt"
			*/

			if (string.IsNullOrEmpty(path))
				return string.Empty;

			var sb = new StringBuilder(path.Length);

			bool previousSlash = true;

			foreach (char c in path) {
				char ch = c == '\\' ? '/' : c;

				if (ch == '/') {
					if (previousSlash)
						continue;

					previousSlash = true;
				}
				else {
					previousSlash = false;
				}

				sb.Append(ch);
			}

			if (sb.Length > 0 && sb[sb.Length - 1] == '/')
				sb.Length--;

			return sb.ToString();
		}

		/// <summary>
		/// Normalizes path part by removing any leading or trailing slashes.
		/// </summary>
		public static string NormalizePart(string part) {
			if (part == null) throw new ArgumentNullException(nameof(part));

			return part.Trim('/', '\\');
		}

		/// <summary>
		/// Checks if path is root folder path, which can be an empty string, null, or the actual root path.
		/// </summary>
		public static bool IsRootPath(string path) {
			return string.IsNullOrEmpty(path) || path == "\\" || path == "/";
		}
	}
}
