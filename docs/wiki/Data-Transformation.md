## Sinks
  - [GZip Compression](Data-Compression.md#gzip-compression)
  - [AES Encryption](Data-Encryption.md#aes-symmetric-encryption)
  - [Rijndael Encryption](Data-Encryption.md#rijndael-symmetric-encryption)
  - [Custom Sinks](Custom-Sinks.md)

## Architecture

![](https://raw.githubusercontent.com/robinrodricks/FluentStorage/develop/.github/sinks-memory.svg)

## Getting Started

Transform sinks is another awesome feature of FluentStorage that works across all the storage providers. Transform sinks allow you to **transform data stream** for both upload and download to somehow transform the underlying stream of data. Examples of transform sinks would be *gzipping data* transparently, *encrypting it*, and so on.

Let's say you would like to *gzip* all of the files that you upload/download to a storage. You can do that in the following way:

```csharp
IBlobStorage myGzippedStorage = StorageFactory.Blobs
   .AzureBlobStorageWithSharedKey("name", "key")
   .WithGzipCompression();
```

Then use the storage as you would before - all the data is compressed as you write it (with any `WriteXXX` method) and decompressed as you read it (with any `ReadXXX` method).


## Implementation

Due to the nature of the transforms, they can change both the underlying data, and stream size, therefore there is an issue with storage providers, as they need to know beforehand the size of the blob you are uploading. The matter becomes more complicated when some implementations need to calculate other statistics of the data before uploading i.e. hash, CRC and so on. Therefore the only *reliable* way to stream transformed data is to actually perform all of the transofrms, and then upload it. In this implementation, FluentStorage uses in-memory transforms to achieve this, however does it extremely efficiently by using [Microsoft.IO.RecyclableMemoryStream](https://github.com/Microsoft/Microsoft.IO.RecyclableMemoryStream) package that performs memory pooling and reclaiming for you so that you don't need to worry about software slowdows. You can read more about this technique [here](http://www.philosophicalgeek.com/2015/02/06/announcing-microsoft-io-recycablememorystream/).

This also means that today a transform sink can upload a stream only as large as the amount of RAM available on your machine. I am, however, thinking of ways to go further than that, and there are some beta implementations available that might see the light soon.
