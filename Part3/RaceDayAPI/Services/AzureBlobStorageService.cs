using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace RaceDayAPI.Services
{
    /// <summary>Uploads images to an Azure Blob Storage container.</summary>
    public class AzureBlobStorageService : IBlobStorageService
    {
        private readonly AzureBlobOptions _options;
        private BlobContainerClient? _container;

        public AzureBlobStorageService(IOptions<AzureBlobOptions> options)
        {
            _options = options.Value;
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
        {
            var container = await GetContainerAsync(cancellationToken);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var blobName = $"{folder.Trim('/')}/{Guid.NewGuid():N}{extension}";
            var blob = container.GetBlobClient(blobName);

            await using var stream = file.OpenReadStream();
            await blob.UploadAsync(
                stream,
                new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType } },
                cancellationToken);

            return blob.Uri.ToString();
        }

        // The container is only contacted when the first image is uploaded, so the API
        // (and the tests) start fine even when Azure has not been configured.
        private async Task<BlobContainerClient> GetContainerAsync(CancellationToken cancellationToken)
        {
            if (_container is not null) return _container;

            if (string.IsNullOrWhiteSpace(_options.ConnectionString))
                throw new InvalidOperationException("Azure Blob Storage is not configured. Set AzureBlob:ConnectionString.");

            var container = new BlobContainerClient(_options.ConnectionString, _options.ContainerName);
            // PublicAccessType.Blob lets a browser show the image from its URL.
            await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

            _container = container;
            return container;
        }
    }
}
