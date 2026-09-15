using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.Persistence;

namespace LegalManager.Infrastructure.Repositories
{
    public class DocumentsRepository : IDocumentRepository
    {
        private readonly LegalManagerDbContext context;

        public DocumentsRepository(LegalManagerDbContext context)
        {
            this.context = context;
        }

        public void Add(Document document) => context.Documents.Add(document);

        public IReadOnlyList<Document> GetAll()
            => context.Documents.Where(d => d.Active).ToList();

        public IReadOnlyList<Document> GetByCaseId(Guid caseId)
            => context.Documents.Where(d => d.CaseId == caseId && d.Active).ToList();

        public Document? GetById(Guid id)
            => context.Documents.FirstOrDefault(d => d.Id == id && d.Active);

        public void Save() => context.SaveChanges();
    }
}
