using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FluentStorage.Storage.Sinks {
	class SinkedBlobStorage : StoreBase {
		private readonly IStore _parent;
		private readonly ITransformSink[] _sinks;

		public SinkedBlobStorage(IStore blobStorage, params ITransformSink[] sinks) {
			if (sinks is null)
				throw new ArgumentNullException(nameof(sinks));

			_parent = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
			_sinks = sinks;
		}
		public override void Dispose() => _parent.Dispose();


		public override Task DeleteObjects(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			return _parent.DeleteObjects(fullPaths, cancellationToken);
		}
		public override Task DeleteObject(string fullPaths, CancellationToken cancellationToken = default) {
			return _parent.DeleteObject(fullPaths, cancellationToken);
		}

		public override Task<List<bool>> ObjectsExists(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			return _parent.ObjectsExists(fullPaths, cancellationToken);
		}
		public override Task<bool> ObjectExists(string fullPath, CancellationToken cancellationToken = default) {
			return _parent.ObjectExists(fullPath, cancellationToken);
		}

		public Task<List<StoreObject>> GetBlobsAsync(IEnumerable<string> fullPaths, CancellationToken cancellationToken = default) {
			return _parent.GetObjectsInfo(fullPaths, cancellationToken);
		}

		public Task<List<StoreObject>> ListObjects(StorageListOptions options = null, CancellationToken cancellationToken = default) {
			return _parent.ListObjects(options, cancellationToken);
		}

		public Task SetBlobsAsync(IEnumerable<StoreObject> blobs, CancellationToken cancellationToken = default) => _parent.SetObjectsInfo(blobs, cancellationToken);

		public override async Task<Stream> OpenRead(string fullPath, CancellationToken cancellationToken = default) {

			//chain streams
			Stream readStream = await _parent.OpenRead(fullPath, cancellationToken).ConfigureAwait(false);

			if (readStream == null)
				return null;

			foreach (ITransformSink sink in _sinks) {
				readStream = sink.OpenReadStream(fullPath, readStream);
			}

			return readStream;
		}

		public override async Task SetObject(string fullPath, Stream dataSourceStream,bool append = false,
		   CancellationToken cancellationToken = default) {
			if (dataSourceStream == null)
				return;

			using (var source = new SinkedStream(dataSourceStream, fullPath, _sinks)) {
				await _parent.SetObject(fullPath, source, append, cancellationToken).ConfigureAwait(false);
			}
		}
		public override async Task SetObject(string fullPath, Stream dataSourceStream, string contentType, bool append, CancellationToken cancellationToken) {
			if (dataSourceStream == null)
				return;

			using (var source = new SinkedStream(dataSourceStream, fullPath, _sinks)) {
				await _parent.SetObject(fullPath, source, contentType, append, cancellationToken).ConfigureAwait(false);
			}
		}

		public async Task RenameAsync(string oldPath, string newPath, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}

	}
}
