Part of the [Data Transformation](Data-Transformation.md) suite of functions.

## AES Symmetric Encryption

This sink implements [symmetric encryption](https://www.venafi.com/blog/what-symmetric-encryption) for upload/download data. I.e. uploaded data is encrypted with a key, and decrypted after download.

It uses [AES](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes?view=net-7.0) encryption with default settings. You control which Key and IV are used.

To add:

```csharp
IBlobStorage storage = StorageFactory.Blobs
   .XXX()
   .WithAesSymmetricEncryption(string encryptionKey, string encryptionSecret)
```

## Rijndael Symmetric Encryption

_**Note: Rijndael is obsolete in .NET 6 and beyond!**_

This sink implements [symmetric encryption](https://www.venafi.com/blog/what-symmetric-encryption) for upload/download data. I.e. uploaded data is encrypted with a key, and decrypted after download.

It uses [Rijndael](https://web.archive.org/web/20070711123800/http://csrc.nist.gov/CryptoToolkit/aes/rijndael/Rijndael-ammended.pdf) encryption with default settings, which is a superset of **AES** encryption algorithm (read about [differences](https://stackoverflow.com/a/748645/80858)). You control which Key and IV are used.

To add:

```csharp
IBlobStorage storage = StorageFactory.Blobs
   .XXX()
   .WithSymmetricEncryption(string encryptionKey, string encryptionSecret)
```

The encryption key is a baase64 encoded binary key. To generate it, you can use the following snippet:

```csharp
void Main()
{
	var cs = new RijndaelManaged();
	cs.GenerateKey();
	string keyBase64 = Convert.ToBase64String(cs.Key);
	
	Console.WriteLine("new encryption key:" + keyBase64);
}
```

Note that it's your own responsibility to store the key securely, make sure it's not put in plaintext anywhere it can be stoken from!