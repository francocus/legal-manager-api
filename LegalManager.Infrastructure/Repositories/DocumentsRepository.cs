using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.Persistence;

namespace LegalManager.Infrastructure.Repositories
{
    public class DocumentsRepository(LegalManagerDbContext context) : IDocumentRepository
    {
        public void Add(Document document) => context.Documents.Add(document);

        public IReadOnlyList<Document> GetAll()
            => [.. context.Documents.Where(d => d.Active)];

        public IReadOnlyList<Document> GetByCaseId(Guid caseId)
            => [.. context.Documents.Where(d => d.CaseId == caseId && d.Active)];

        public Document? GetById(Guid id)
            => context.Documents.FirstOrDefault(d => d.Id == id && d.Active);

        public void Save() => context.SaveChanges();
    }
}
