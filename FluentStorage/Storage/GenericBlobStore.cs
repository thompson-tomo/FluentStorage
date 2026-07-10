using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentStorage.Storage {
	/// <summary>
	/// Provides the most generic form of the blob storage implementation
	/// </summary>
	public abstract class GenericBlobStore : IBucket {

		protected abstract bool CanListHierarchy { get; }

		public virtual async Task<IReadOnlyCollection<StorageObject>> ListAsync(ListOptions options = null, CancellationToken cancellationToken = default) {
			var result = new List<StorageObject>();
			if (options == null) options = new ListOptions();

			await ListStepAsync(options.FolderPath, options, result, cancellationToken).ConfigureAwait(false);

			if (options.MaxResults != null && result.Count > options.MaxResults.Value) {
				result = result.Take(options.MaxResults.Value).ToList();
			}

			return result;
		}

		private async Task ListStepAsync(string path, ListOptions options, List<StorageObject> container, CancellationToken cancellationToken) {
			IReadOnlyCollection<StorageObject> chunk = await ListAtAsync(path, options, cancellationToken).ConfigureAwait(false);

			if (options.BrowseFilter != null) {
				container.AddRange(chunk.Where(b => options.BrowseFilter(b)));
			}
			else {
				container.AddRange(chunk);
			}

			if (options.MaxResults != null && container.Count >= options.MaxResults.Value)
				return;

			if (!CanListHierarchy && options.Recurse) {
				await Task.WhenAll(
				   chunk.Where(c => c.IsFolder).ToList()
				   .Select(c => ListStepAsync(c.FullPath, options, container, cancellationToken))).ConfigureAwait(false);
			}
		}

		protected virtual Task<IReadOnlyCollection<StorageObject>> ListAtAsync(string path, ListOptions options, CancellationToken cancellationToken) {
			throw new NotSupportedException();
		}

		public virtual Task DeleteAsync(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			return Task.WhenAll(fullPaths.Select(fp => DeleteSingleAsync(fp, cancellationToken)));
		}

		protected virtual Task DeleteSingleAsync(string fullPath, CancellationToken cancellationToken) {
			throw new NotSupportedException();
		}

		public virtual async Task<IReadOnlyCollection<bool>> ExistsAsync(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			return await Task.WhenAll(fullPaths.Select(fp => ExistsAsync(fp, cancellationToken))).ConfigureAwait(false);
		}
		protected virtual Task<bool> ExistsAsync(string fullPath, CancellationToken cancellationToken) {
			throw new NotSupportedException();
		}

		public async Task<IReadOnlyCollection<StorageObject>> GetBlobsAsync(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			return await Task.WhenAll(fullPaths.Select(fp => GetBlobAsync(fp, cancellationToken))).ConfigureAwait(false);
		}

		protected virtual Task<StorageObject> GetBlobAsync(string fullPath, CancellationToken cancellationToken) => throw new NotSupportedException();

		public virtual Task<Stream> OpenReadAsync(string fullPath, CancellationToken cancellationToken = default) {
			throw new NotSupportedException();
		}

		public Task<ITransaction> OpenTransactionAsync() => throw new NotSupportedException();

		public virtual async Task WriteAsync(string fullPath, Stream sourceStream, string contentType, bool append, CancellationToken cancellationToken) {
			await WriteAsync(fullPath, sourceStream, null, append, cancellationToken).ConfigureAwait(false);
		}

		public virtual Task WriteAsync(string fullPath, Stream dataStream, bool append = false, CancellationToken cancellationToken = default) {
			throw new NotSupportedException();
		}

		public virtual Task SetBlobsAsync(IEnumerable<StorageObject> blobs, CancellationToken cancellationToken = default) => throw new NotSupportedException();

		public virtual void Dispose() {

		}

	}
}
