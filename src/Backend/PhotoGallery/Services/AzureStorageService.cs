using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MimeMapping;

namespace PhotoGallery.Services;

public class AzureStorageService
{
    private readonly BlobServiceClient blobServiceClient;
    private readonly string containerName;

    public AzureStorageService(IConfiguration configuration)
    {
        blobServiceClient = new BlobServiceClient(configuration.GetConnectionString("AzureStorageConnection"));
        containerName = configuration.GetValue<string>("AppSettings:ContainerName").ToLowerInvariant();
    }

    public async Task SaveAsync(string path, Stream stream)
    {
        var blobClient = await GetBlobClientAsync(path, true);

        stream.Position = 0;
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = MimeUtility.GetMimeMapping(path) });
    }

    public async Task<Stream?> ReadAsync(string path)
    {
        var blobClient = await GetBlobClientAsync(path);

        var blobExists = await blobClient.ExistsAsync();
        if (!blobExists)
        {
            return null;
        }

        var stream = await blobClient.OpenReadAsync();
        return stream;
    }

    public async Task DeleteAsync(string path)
    {
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await blobContainerClient.DeleteBlobIfExistsAsync(path);
    }

    private async Task<BlobClient> GetBlobClientAsync(string blobName, bool createIfNotExists = false)
    {
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
        if (createIfNotExists)
        {
            await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);
        }

        var blobClient = blobContainerClient.GetBlobClient(blobName.ToLowerInvariant());
        return blobClient;
    }
}
