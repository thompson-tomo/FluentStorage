using FluentStorage.Enums;
using FluentStorage.Utils.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FluentStorage.Storage {
	/// <summary>
	/// Allows to combine several storage providers (or even of the same type) in one virtual storage interface.
	/// Providers are distinguished using a prefix. Essentially this allows to mount providers in a virtual filesystem.
	/// </summary>
	public class VirtualStorage : IVirtualStorage {
		private readonly ConcurrentDictionary<string, HashSet<StorageObject>> _pathToMountBlobs = new ConcurrentDictionary<string, HashSet<StorageObject>>();
		private readonly List<StorageObject> _mountPoints = new List<StorageObject>();

		/// <summary>
		/// Creates an instance
		/// </summary>
		public VirtualStorage() {

		}

		/// <summary>
		/// Mounts a storage to virtual path
		/// </summary>
		/// <param name="path"></param>
		/// <param name="storage"></param>
		public void Mount(string path, IBucket storage) {
			if (path is null)
				throw new ArgumentNullException(nameof(path));
			if (storage is null)
				throw new ArgumentNullException(nameof(storage));

			path = StoragePath.Normalize(path);

			_mountPoints.Add(new StorageObject(path) { Tag = storage });

			string absPath = null;

			string[] parts = StoragePath.Split(path);

			if (parts.Length == 0)   //mount at root
			{
				MountPath(path, storage, true);
			}
			else {
				for (int i = 0; i < parts.Length; i++) {
					absPath = StoragePath.Combine(absPath, parts[i]);

					MountPath(absPath, storage, i == parts.Length - 1);
				}
			}
		}

		private void MountPath(string path, IBucket storage, bool isMountPoint) {
			string containerPath = StoragePath.IsRootPath(path) ? path : StoragePath.GetParent(path);

			if (!_pathToMountBlobs.TryGetValue(containerPath, out HashSet<StorageObject> blobs)) {
				blobs = new HashSet<StorageObject>();
				_pathToMountBlobs[containerPath] = blobs;
			}

			// this is the mount
			if (isMountPoint) {
				var mountBlob = new StorageObject(path, StorageObjectType.Folder) { Tag = storage };
				mountBlob.TryAddProperties("IsMountPoint", true);
				blobs.Add(mountBlob);
			}
			else {
				var intBlob = new StorageObject(path, StorageObjectType.Folder);
				blobs.Add(intBlob);
			}
		}

		class MpTag : MpTag<object> {
		}

		class MpTag<T> {
			public string fullPath;
			public string relPath;
			public T result;
		}

		class XTag<TInput, TReducedInput, TOutput> {
			public string fullPath;
			public string relPath;
			public TInput fullInput;
			public TReducedInput reducedInput;
			public TOutput result;
		}

		private Dictionary<IBucket, List<XTag<TInput, TReducedInput, TOutput>>> Explode<TInput, TReducedInput, TOutput>(
		   IEnumerable<TInput> inputs,
		   Func<TInput, string> inputToFullPathReducer,
		   Func<TInput, string, string, TReducedInput> inputReducer) {
			var result = new Dictionary<IBucket, List<XTag<TInput, TReducedInput, TOutput>>>();

			foreach (TInput input in inputs) {
				string fullPath = inputToFullPathReducer(input);

				if (TryExplodeToMountPoint(fullPath, out IBucket storage, out string relPath)) {
					if (!result.TryGetValue(storage, out List<XTag<TInput, TReducedInput, TOutput>> acc)) {
						acc = new List<XTag<TInput, TReducedInput, TOutput>>();
						result[storage] = acc;
					}

					var tag = new XTag<TInput, TReducedInput, TOutput> {
						fullPath = fullPath,
						relPath = relPath,
						fullInput = input,
						reducedInput = inputReducer(input, fullPath, relPath),
						result = default
					};

					acc.Add(tag);
				}
			}

			return result;
		}

		private Dictionary<IBucket, List<MpTag<T>>> Explode<T>(
		   IEnumerable<string> fullPaths,
		   out Dictionary<string, MpTag<T>> fullPathToTag) {
			var rmap = new Dictionary<IBucket, List<MpTag<T>>>();
			fullPathToTag = new Dictionary<string, MpTag<T>>();

			foreach (string fp in fullPaths) {
				if (!TryExplodeToMountPoint(fp, out IBucket storage, out string relPath)) {
					fullPathToTag[fp] = null;
				}
				else {
					if (!rmap.TryGetValue(storage, out List<MpTag<T>> tags)) {
						tags = new List<MpTag<T>>();
						rmap[storage] = tags;
					}

					var tag = new MpTag<T> { fullPath = fp, relPath = relPath };

					tags.Add(tag);
					fullPathToTag[fp] = tag;
				}
			}
			return rmap;
		}

		/// <summary>
		/// Simpler version of Explode that does not need to match to the result
		/// </summary>
		private Dictionary<IBucket, List<string>> Explode(IEnumerable<string> fullPaths) {
			var map = new Dictionary<IBucket, List<string>>();

			foreach (string fp in fullPaths) {
				if (TryExplodeToMountPoint(fp, out IBucket storage, out string relPath)) {
					if (!map.TryGetValue(storage, out List<string> relPaths)) {
						relPaths = new List<string>();
						map[storage] = relPaths;
					}

					relPaths.Add(relPath);
				}
			}
			return map;
		}

		private async Task ExecuteAsync(
		   IEnumerable<string> fullPaths,
		   Func<IBucket, IEnumerable<string>, Task> action) {
			Dictionary<IBucket, List<string>> map = Explode(fullPaths);

			IEnumerable<Task> tasks = map.Select(pair => action(pair.Key, pair.Value));

			await Task.WhenAll(tasks).ConfigureAwait(false);
		}

		private async Task ExecuteAsync(
		   IEnumerable<StorageObject> blobs,
		   Func<IBucket, IEnumerable<StorageObject>, Task> action) {
			Dictionary<IBucket, List<XTag<StorageObject, StorageObject, bool>>> map = Explode<StorageObject, StorageObject, bool>(blobs,
			   b => b.FullPath,
			   (b, f, r) => {
				   StorageObject reduced = (StorageObject)b.Clone();
				   reduced.SetFullPath(r);
				   return reduced;
			   });

			foreach (KeyValuePair<IBucket, List<XTag<StorageObject, StorageObject, bool>>> pair in map) {
				IEnumerable<StorageObject> relBlobs = pair.Value.Select(x => x.reducedInput);

				await action(pair.Key, relBlobs).ConfigureAwait(false);
			}
		}


		private async Task<IReadOnlyCollection<TResult>> ExecuteAsync<TResult>(
		   IEnumerable<string> fullPaths,
		   Func<IBucket, IEnumerable<string>, Task<IReadOnlyCollection<TResult>>> action) {
			Dictionary<IBucket, List<MpTag<TResult>>> dic = Explode(
			   fullPaths,
			   out Dictionary<string, MpTag<TResult>> fullPathToTag);

			// execute and assign result
			foreach (KeyValuePair<IBucket, List<MpTag<TResult>>> pair in dic) {
				IEnumerable<string> rps = pair.Value.Select(v => v.relPath);

				IReadOnlyCollection<TResult> br = await action(pair.Key, rps).ConfigureAwait(false);

				foreach (Tuple<TResult, MpTag<TResult>> doublePair in EnumerableExtensions.MultiIterate(br, pair.Value)) {
					doublePair.Item2.result = doublePair.Item1;
				}
			}

			// collect full result
			return fullPaths.Select(fp => fullPathToTag[fp].result).ToList();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="fullPaths"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public virtual Task DeleteAsync(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			return ExecuteAsync(fullPaths, (storage, paths) => storage.DeleteAsync(paths, cancellationToken));
		}


		public virtual void Dispose() {

		}

		public virtual Task<IReadOnlyCollection<bool>> ExistsAsync(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			return ExecuteAsync(
			   fullPaths,
			   (storage, fps) => storage.ExistsAsync(fps, cancellationToken));
		}

		public virtual Task<IReadOnlyCollection<StorageObject>> GetBlobsAsync(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			return ExecuteAsync(
			   fullPaths,
			   (storage, fps) => storage.GetBlobsAsync(fps, cancellationToken));
		}

		public async virtual Task<IReadOnlyCollection<StorageObject>> ListAsync(ListOptions options = null, CancellationToken cancellationToken = default) {
			if (options == null)
				options = new ListOptions();

			var result = new List<StorageObject>();

			//mount folders/points
			if (_pathToMountBlobs.TryGetValue(options.FolderPath, out HashSet<StorageObject> mounts)) {
				foreach (StorageObject blob in mounts) {
					if (blob.Tag == null) {
						result.Add(blob);
					}
					else {
						//mountPoints.Add(blob);

						if (!StoragePath.IsRootPath(blob.FullPath)) {
							result.Add(blob);
						}
					}
				}
			}

			/*
			 * abs path:
			 * /f1/f2/f3/f4/f5
			 *
			 * mount: /f1
			 * list:  /f1/f2/f3/f4
			 *
			 * strip mount - /f2/f3/f4 - list from mount
			 *
			 *
			 */

			//find mount points

			List<StorageObject> mountPoints = _mountPoints.Where(mp => options.FolderPath.StartsWith(mp.FullPath)).ToList();

			foreach (StorageObject mountPoint in mountPoints) {
				IBucket storage = (IBucket)mountPoint.Tag;

				string relPath = options.FolderPath.Substring(mountPoint.FullPath.Length);

				ListOptions mountOptions = options.Clone();
				mountOptions.FolderPath = StoragePath.Normalize(relPath);

				IReadOnlyCollection<StorageObject> mountResults = await storage.ListAsync(mountOptions, cancellationToken).ConfigureAwait(false);
				foreach (StorageObject blob in mountResults) {
					blob.PrependPath(mountPoint.FullPath);
				}
				result.AddRange(mountResults);

				// check that we reached the limit in options, and if so - trim result we have and break
				if (options.MaxResults != null) {
					int max = options.MaxResults.Value;
					if (result.Count >= max) {
						result = result.Take(max).ToList();
						break;
					}
				}
			}

			return result;
		}

		public virtual async Task<Stream> OpenReadAsync(string fullPath, CancellationToken cancellationToken = default) {
			if (!TryExplodeToMountPoint(fullPath, out IBucket storage, out string relPath))
				return null;

			return await storage.OpenReadAsync(relPath, cancellationToken).ConfigureAwait(false);
		}


		public virtual Task SetBlobsAsync(IEnumerable<StorageObject> blobs, CancellationToken cancellationToken = default) {
			return ExecuteAsync(blobs, (s, rb) => s.SetBlobsAsync(rb, cancellationToken));
		}

		private bool TryExplodeToMountPoint(string fullPath, out IBucket storage, out string relPath) {
			storage = null;
			relPath = null;

			if (fullPath == null)
				return false;

			fullPath = StoragePath.Normalize(fullPath);

			StorageObject mountPoint = _mountPoints.FirstOrDefault(mp => fullPath.StartsWith(mp.FullPath));
			if (mountPoint == null)
				return false;

			storage = (IBucket)mountPoint.Tag;
			relPath = StoragePath.Normalize(fullPath.Substring(mountPoint.FullPath.Length));
			return true;
		}

		public virtual Task<ITransaction> OpenTransactionAsync() => null;

		public virtual async Task WriteAsync(string fullPath, Stream dataStream, bool append = false, CancellationToken cancellationToken = default) {
			if (!TryExplodeToMountPoint(fullPath, out IBucket storage, out string relPath))
				return;


			await storage.WriteAsync(relPath, dataStream, append, cancellationToken).ConfigureAwait(false);
		}
		public async Task WriteAsync(string fullPath, Stream dataStream, string contentType, bool append, CancellationToken cancellationToken) {
			if (!TryExplodeToMountPoint(fullPath, out IBucket storage, out string relPath))
				return;


			await storage.WriteAsync(relPath, dataStream, contentType, append, cancellationToken).ConfigureAwait(false);
		}
	}
}
