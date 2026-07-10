Part of the [Data Transformation](Data-Transformation.md) suite of functions.

## Gzip Compression

To create the sink, call extension method `WithGzipCompression` and optionally pass a *compression level* which defaults to `Optimal`:

```csharp
IBlobStorage storage = StorageFactory.Blobs
   .XXX()
   .WithGzipCompression(CompressionLevel compressionLevel = CompressionLevel.Optimal)
```
