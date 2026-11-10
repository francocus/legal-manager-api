namespace LegalManager.Domain.Interfaces
{
    public interface IFileStorage
    {
        Task<string> SaveAsync(Guid caseId, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default);

        Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);

        Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(string storagePath, CancellationToken cancellationToken = default);
    }
}
