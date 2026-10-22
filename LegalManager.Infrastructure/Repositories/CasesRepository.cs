using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalManager.Infrastructure.Repositories
{
    public class CasesRepository(LegalManagerDbContext context) : ICaseRepository
    {
        public void Add(Case caseItem) => context.Cases.Add(caseItem);

        public async Task<IReadOnlyList<Case>> GetAllAsync(CancellationToken cancellationToken = default)
            => await context.Cases.Where(c => c.Active).ToListAsync(cancellationToken);

        public async Task<Case?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await context.Cases.FirstOrDefaultAsync(c => c.Id == id && c.Active, cancellationToken);

        public Task SaveAsync(CancellationToken cancellationToken = default)
            => context.SaveChangesAsync(cancellationToken);
    }
}