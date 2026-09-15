using LegalManager.Domain.Entities;

namespace LegalManager.Domain.Interfaces
{
    public interface IDocumentRepository
    {
        void Add(Document document);

        IReadOnlyList<Document> GetAll();

        IReadOnlyList<Document> GetByCaseId(Guid caseId);

        Document? GetById(Guid id);

        void Save();
    }
}
