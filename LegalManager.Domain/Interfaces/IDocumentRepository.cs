using LegalManager.Domain.Entities;

namespace LegalManager.Domain.Interfaces
{
    public interface IDocumentRepository
    {
        void Add(Document document);

        Task SaveAsync(CancellationToken cancellationToken = default);

        IReadOnlyList<Document> GetAll();

        IReadOnlyList<Document> GetByCaseId(Guid caseId);

        Document? GetById(Guid id);

        void Save();
    }
}
