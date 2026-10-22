using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalManager.Infrastructure.Repositories
{
    public class DocumentsRepository(LegalManagerDbContext context) : IDocumentRepository
    {
        public void Add(Document document) => context.Documents.Add(document);

        public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken = default)
            => await context.Documents.Where(d => d.Active).ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<Document>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default)
            => await context.Documents.Where(d => d.CaseId == caseId && d.Active).ToListAsync(cancellationToken);

        public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await context.Documents.FirstOrDefaultAsync(d => d.Id == id && d.Active, cancellationToken);

        public Task SaveAsync(CancellationToken cancellationToken = default)
            => context.SaveChangesAsync(cancellationToken);
    }
}
