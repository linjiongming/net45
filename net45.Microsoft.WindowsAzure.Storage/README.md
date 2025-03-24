# net45.Microsoft.WindowsAzure.Storage



## Usage

```csharp
using Microsoft.WindowsAzure.Storage;

// DI
services.AddCloudBlobContainer("StorageConnectionString", "StorageContainerName");


private readonly CloudBlobContainer _blobContainer;

ctor(CloudBlobContainer blobContainer)
{
    _blobContainer = blobContainer;
}

// Upload
await _blobContainer.UploadAsync(fileBytes, "some/folder/filename.txt", cancellation);

// Download
byte[] fileBytes = await _blobContainer.GetBytesAsync("some/folder/filename.txt", cancellation);
```

