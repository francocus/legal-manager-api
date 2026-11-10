using LegalManager.Domain.Interfaces;

namespace LegalManager.Infrastructure.Storage
{
    public class LocalFileSystemStorage(string rootPath) : IFileStorage
    {
        private readonly string _rootPath = rootPath;

        public async Task<string> SaveAsync(Guid caseId, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            var caseFolder = Path.Combine(_rootPath, caseId.ToString());
            Directory.CreateDirectory(caseFolder);

            var documentId = Guid.NewGuid();
            var storedFileName = $"{documentId}_{fileName}";
            var fullPath = Path.Combine(caseFolder, storedFileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await content.CopyToAsync(stream, cancellationToken);

            return fullPath;
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(storagePath))
                throw new FileNotFoundException("El archivo no se encuentra en el servidor local.", storagePath);

            Stream stream = new FileStream(storagePath, FileMode.Open, FileAccess.Read);
            return Task.FromResult(stream);
        }

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            try
            {
                if (File.Exists(storagePath))
                    File.Delete(storagePath);
            }
            catch (IOException) { }

            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string storagePath, CancellationToken cancellationToken = default)
            => Task.FromResult(File.Exists(storagePath));
    }
}
