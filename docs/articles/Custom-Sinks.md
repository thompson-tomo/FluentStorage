Part of the [Data Transformation](Data-Transformation.md) suite of functions.

## Build Your Own Sink

Implementing your own transformation sink is a matter of implementing a new class derived from [`ITransformSink`](xref:FluentStorage.Storage.Sinks.ITransformSink) interface, which only has two methods:

```csharp
   public interface ITransformSink
   {
      Stream OpenReadStream(string fullPath, Stream parentStream);

      Stream OpenWriteStream(string fullPath, Stream parentStream);
   }
```

The first one is called when FluentStorage opens a blob for reading, so that you can replace original stream passed in `parentStream` with your own. The second one does the reverse. For instance, have a look at the implementation of `Gzip` sink, as it's the easiest one:

```csharp
public class GZipSink : ITransformSink
{
   private readonly CompressionLevel _compressionLevel;

   public GZipSink(CompressionLevel compressionLevel = CompressionLevel.Optimal)
   {
      _compressionLevel = compressionLevel;
   }

   public Stream OpenReadStream(string fullPath, Stream parentStream)
   {
      if(parentStream == null)
         return null;

      return new GZipStream(parentStream, CompressionMode.Decompress, false);
   }

   public Stream OpenWriteStream(string fullPath, Stream parentStream)
   {
      return new GZipStream(parentStream, _compressionLevel, false);
   }
}
```

This sink simply takes incoming stream and wraps it around in the standard built-in `GZipStream` from `System.IO.Compression` namespace.

## Passing Your Sink To Storage

In order to use the sink, you can simply call `.WithSinks` extension method and pass the sink you want to use. For instance, to enable GZipSink do the following:

```csharp
IBlobStorage storage = StorageFactory.Blobs
   .XXX()
   .WithSinks(new GZipSink());
```

You can also create an extension method if you use this often:

```csharp
public static IBlobStorage WithGzipCompression(
   this IBlobStorage blobStorage, CompressionLevel compressionLevel = CompressionLevel.Optimal)
{
   return blobStorage.WithSinks(new GZipSink(compressionLevel));
}
```

## Chaining Sinks

`.WithSinks` extension method in fact accept an array of sinks, which means that sinks can be chained together. This is useful when you need to do multiple transformations at the same time. For instance, if I would like to both compress, and encrypt data in the target storage, I could initialise my storage in the following way:

```csharp
IBlobStorage encryptedAndCompressed =          
   StorageFactory.Blobs
      .InMemory()
      .WithSinks(
         new GZipSink(),
         new SymmetricEncryptionSink("To6X5XVaNNMKFfxssJS6biREGpOVZjEIC6T7cc1rJF0=")))
```

Note that declaration order matters here - when writing, the data is **compressed first**, and **encrypted second**.
