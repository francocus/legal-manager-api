using LegalManager.Domain.Entities;

namespace LegalManager.Domain.Interfaces
{
    public interface IDocumentRepository
    {
        void Add(Document document);

        Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Document>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default);

        Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task SaveAsync(CancellationToken cancellationToken = default);
    }
}
