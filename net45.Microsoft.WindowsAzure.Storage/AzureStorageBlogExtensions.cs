using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage.Blob;
using MimeKit;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage
{
    public static class AzureStorageBlogExtensions
    {
        public static IServiceCollection AddCloudBlobContainer(this IServiceCollection services, string connectionString, string containerName)
        {
            // Azure 的默认最低 TLS 版本为 TLS 1.2， net45必须手动开启
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient client = account.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(containerName);
            services.AddSingleton(container);
            return services;
        }

        public static CloudBlob GetBlob(this CloudBlobContainer container, string filename)
        {
            return container.GetBlobReference(filename);
        }

        public static byte[] GetBytes(this CloudBlobContainer container, string filename)
        {
            CloudBlob blob = container.GetBlob(filename);
            byte[] bytes = new byte[blob.Properties.Length];
            int length = 1024 * 1024 * 4; // 4m
            int count = 0;
            if (blob.Properties.Length > length)
            {
                while (count < blob.Properties.Length)
                {
                    count += blob.DownloadRangeToByteArray(bytes, count, count, length);
                }
            }
            else
            {
                count = blob.DownloadToByteArray(bytes, 0);
            }
            return bytes;
        }

        public static async Task<byte[]> GetBytesAsync(this CloudBlobContainer container, string filename, CancellationToken cancellation = default(CancellationToken))
        {
            CloudBlob blob = container.GetBlob(filename);
            if (!blob.Exists()) return null;
            byte[] bytes = new byte[blob.Properties.Length];
            int length = 1024 * 1024 * 4; // 4m
            int count = 0;
            if (blob.Properties.Length > length)
            {
                while (count < blob.Properties.Length)
                {
                    count += await blob.DownloadRangeToByteArrayAsync(bytes, count, count, length, cancellation);
                }
            }
            else
            {
                count = await blob.DownloadToByteArrayAsync(bytes, 0, cancellation);
            }
            return bytes;
        }

        public static CloudBlockBlob GetBlockBlob(this CloudBlobContainer container, string filename)
        {
            return container.GetBlockBlobReference(filename);
        }

        public static CloudBlockBlob Upload(this CloudBlobContainer container, Stream stream, string filename, string mimeType = null)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(filename);
            blockBlob.Properties.ContentType = mimeType ?? MimeTypes.GetMimeType(filename);
            blockBlob.UploadFromStream(stream);
            return blockBlob;
        }

        public static CloudBlockBlob Upload(this CloudBlobContainer container, byte[] buffer, string filename, string mimeType = null)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(filename);
            blockBlob.Properties.ContentType = mimeType ?? MimeTypes.GetMimeType(filename);
            blockBlob.UploadFromByteArray(buffer, 0, buffer.Length);
            return blockBlob;
        }

        public static async Task<CloudBlockBlob> UploadAsync(this CloudBlobContainer container, Stream stream, string blobName, CancellationToken cancellation = default(CancellationToken))
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
            blockBlob.Properties.ContentType = MimeTypes.GetMimeType(blobName);
            await blockBlob.UploadFromStreamAsync(stream, cancellation);
            return blockBlob;
        }

        public static async Task<CloudBlockBlob> UploadAsync(this CloudBlobContainer container, byte[] buffer, string blobName, CancellationToken cancellation = default(CancellationToken))
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
            blockBlob.Properties.ContentType = MimeTypes.GetMimeType(blobName);
            await blockBlob.UploadFromByteArrayAsync(buffer, 0, buffer.Length, cancellation);
            return blockBlob;
        }

        public static void DeleteDirectoty(this CloudBlobContainer container, string directoty)
        {
            foreach (CloudBlockBlob blob in container.GetDirectoryReference(directoty).ListBlobs(true).OfType<CloudBlockBlob>())
            {
                blob.DeleteIfExists();
            }
        }

        public static bool TryGet(this CloudBlobContainer container, string filename, out CloudBlockBlob blockBlob)
        {
            blockBlob = container.GetBlockBlobReference(filename);
            return blockBlob.Exists();
        }

        public static string GetSas(this CloudBlobContainer container, DateTimeOffset? startTime, DateTimeOffset? expiryTime)
        {
            SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
            {
                Permissions =
                    SharedAccessBlobPermissions.Read |
                    SharedAccessBlobPermissions.Write |
                    SharedAccessBlobPermissions.Delete |
                    SharedAccessBlobPermissions.List |
                    SharedAccessBlobPermissions.Add |
                    SharedAccessBlobPermissions.Create,
                SharedAccessStartTime = startTime,
                SharedAccessExpiryTime = expiryTime
            };
            
            string token = container.GetSharedAccessSignature(policy);

            return token;
        }

        public static string GetAbsoluteUri(this CloudBlobContainer container, string relativeUri)
        {
            return container.Uri.ToString().TrimEnd('/') + relativeUri.TrimStart('/');
        }
    }
}