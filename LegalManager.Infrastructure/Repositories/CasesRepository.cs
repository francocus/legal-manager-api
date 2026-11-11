using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalManager.Infrastructure.Repositories
{
    public class CasesRepository(LegalManagerDbContext context) : GenericRepository<Case>(context), ICaseRepository
    {
        // Same Active filter as GetAllAsync, ordered by Id so pages are stable,
        // with the total counted over the same filter (before Skip/Take).
        public async Task<(IReadOnlyList<Case> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(c => c.Active).OrderBy(c => c.Id);

            var total = await query.CountAsync(cancellationToken);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

            return (items, total);
        }
        public async Task<bool> HasAnyCaseAsClientAsync(Guid clientId, CancellationToken cancellationToken = default)
            => await _dbSet.AnyAsync(c => c.ClientId == clientId, cancellationToken);

        // CaseLawyer is a shared-type join entity: no CLR type and no typed DbSet, so it is
        // queried by name through the property-bag set, reading the shadow FK by name.
        public async Task<bool> HasAnyCaseAsLawyerAsync(Guid lawyerId, CancellationToken cancellationToken = default)
            => await _context.Set<Dictionary<string, object>>("CaseLawyer")
                .AnyAsync(cl => EF.Property<Guid>(cl, "LawyerId") == lawyerId, cancellationToken);
    }
}
