using LegalManager.Domain.Entities;
using LegalManager.Domain.Enums;

namespace LegalManager.Domain.Interfaces
{
    public interface IDocumentRepository : IRepository<Document>
    {
        Task<IReadOnlyList<Document>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Document>> GetInactiveAsync(CancellationToken cancellationToken = default);

        Task<Document?> GetByIdIncludingInactiveAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
