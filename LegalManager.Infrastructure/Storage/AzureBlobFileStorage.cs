using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LegalManager.Domain.Interfaces;

namespace LegalManager.Infrastructure.Storage
{
    public class AzureBlobFileStorage : IFileStorage
    {
        private readonly BlobContainerClient _containerClient;

        public AzureBlobFileStorage(string connectionString, string containerName = "documents")
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("La Connection String de Azure Storage no puede estar vacía.", nameof(connectionString));

            var serviceClient = new BlobServiceClient(connectionString);
            _containerClient = serviceClient.GetBlobContainerClient(string.IsNullOrWhiteSpace(containerName) ? "documents" : containerName);
            _containerClient.CreateIfNotExists(PublicAccessType.None);
        }

        public async Task<string> SaveAsync(Guid caseId, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            var documentId = Guid.NewGuid();
            var blobName = $"{caseId}/{documentId}_{fileName}";
            var blobClient = _containerClient.GetBlobClient(blobName);

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/pdf" : contentType
                }
            };

            await blobClient.UploadAsync(content, uploadOptions, cancellationToken);
            return blobName;
        }

        public async Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            var blobClient = _containerClient.GetBlobClient(storagePath);
            if (!await blobClient.ExistsAsync(cancellationToken))
                throw new FileNotFoundException("El archivo no se encuentra en Azure Blob Storage.", storagePath);

            var download = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return download.Value.Content;
        }

        public async Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            try
            {
                var blobClient = _containerClient.GetBlobClient(storagePath);
                await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            }
            catch (Exception)
            {
                // Best-effort cleanup
            }
        }

        public async Task<bool> ExistsAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            var blobClient = _containerClient.GetBlobClient(storagePath);
            return await blobClient.ExistsAsync(cancellationToken);
        }
    }
}
