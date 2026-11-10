using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalManager.Infrastructure.Repositories
{
    public class DocumentsRepository(LegalManagerDbContext context) : GenericRepository<Document>(context), IDocumentRepository
    {
        public async Task<IReadOnlyList<Document>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default)
            => await _dbSet.Where(d => d.CaseId == caseId && d.Active).ToListAsync(cancellationToken);
    }
}
