using System;
using System.Collections.Generic;
using FluentStorage.Model;
using FluentStorage.Enums;

namespace FluentStorage.Rules {

	/// <summary>
	/// Only accept objects of the given name, or exclude objects of a given name.
	/// Originally from FluentFTP `FtpFileNameRule`.
	/// </summary>
	public class ObjectNameRule : StorageRule {

		/// <summary>
		/// If true, only objects of the given name are uploaded or downloaded. If false, objects of the given name are excluded.
		/// </summary>
		public bool Whitelist { get; set; }

		/// <summary>
		/// The objects names to match
		/// </summary>
		public IList<string> Names { get; set; }

		/// <summary>
		/// Only accept objects of the given name, or exclude objects of a given name.
		/// </summary>
		/// <param name="whitelist">If true, only objects of the given name are downloaded. If false, objects of the given name are excluded.</param>
		/// <param name="names">The objects names to match</param>
		public ObjectNameRule(bool whitelist, IList<string> names) {
			this.Whitelist = whitelist;
			this.Names = names;
		}

		/// <summary>
		/// Checks if the objects contain the given name, or exclude objects of the given name.
		/// </summary>
		public override bool IsAllowed(StoreObject item) {
			if (item.Type == StorageObjectType.File) {
				var fileName = item.Name;
				if (Whitelist) {
					return Names.Contains(fileName);
				}
				else {
					return !Names.Contains(fileName);
				}
			}
			else {
				return true;
			}
		}

	}
}