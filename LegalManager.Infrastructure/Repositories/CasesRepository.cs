using LegalManager.Domain.Entities;
using LegalManager.Domain.Enums;
using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalManager.Infrastructure.Repositories
{
    public class CasesRepository(LegalManagerDbContext context) : GenericRepository<Case>(context), ICaseRepository
    {
        public async Task<(IReadOnlyList<Case> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(c => c.Active).OrderBy(c => c.Id);

            var total = await query.CountAsync(cancellationToken);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task<bool> HasAnyCaseAsClientAsync(Guid clientId, CancellationToken cancellationToken = default)
            => await _dbSet.AnyAsync(c => c.ClientId == clientId, cancellationToken);

        public async Task<bool> HasAnyCaseAsLawyerAsync(Guid lawyerId, CancellationToken cancellationToken = default)
            => await _context.Set<Dictionary<string, object>>("CaseLawyer")
                .AnyAsync(cl => EF.Property<Guid>(cl, "LawyerId") == lawyerId, cancellationToken);

        public async Task<IReadOnlyList<Case>> GetInactiveAsync(CancellationToken cancellationToken = default)
            => await _dbSet.Where(c => !c.Active)
                .OrderByDescending(c => c.DeactivatedAt == null)
                .ThenByDescending(c => c.DeactivatedAt)
                .ToListAsync(cancellationToken);

        public async Task<Case?> GetByIdIncludingInactiveAsync(Guid id, CancellationToken cancellationToken = default)
            => await _dbSet.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        public async Task<IReadOnlyDictionary<Guid, int>> CountAppointmentsByCaseIdsAsync(IReadOnlyCollection<Guid> caseIds, CancellationToken cancellationToken = default)
        {
            if (caseIds.Count == 0) return new Dictionary<Guid, int>();

            return await _context.Set<Appointment>()
                .Where(a => a.CaseId.HasValue && caseIds.Contains(a.CaseId.Value))
                .GroupBy(a => a.CaseId!.Value)
                .Select(g => new { CaseId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CaseId, x => x.Count, cancellationToken);
        }

        public async Task<IReadOnlyDictionary<Guid, int>> CountDocumentsByCaseIdsAsync(IReadOnlyCollection<Guid> caseIds, CancellationToken cancellationToken = default)
        {
            if (caseIds.Count == 0) return new Dictionary<Guid, int>();

            return await _context.Set<Document>()
                .Where(d => caseIds.Contains(d.CaseId))
                .GroupBy(d => d.CaseId)
                .Select(g => new { CaseId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CaseId, x => x.Count, cancellationToken);
        }

        public async Task<IReadOnlyDictionary<Guid, (string CaseNumber, string Title)>> GetCaseHeadersByIdsAsync(IReadOnlyCollection<Guid> caseIds, CancellationToken cancellationToken = default)
        {
            if (caseIds.Count == 0) return new Dictionary<Guid, (string, string)>();

            var items = await _dbSet
                .Where(c => caseIds.Contains(c.Id))
                .Select(c => new { c.Id, c.CaseNumber, c.Title })
                .ToListAsync(cancellationToken);

            return items.ToDictionary(c => c.Id, c => (c.CaseNumber, c.Title));
        }

        public async Task<IReadOnlyList<Guid>> GetCaseIdsForLawyerAsync(Guid lawyerId, CancellationToken cancellationToken = default)
            => await _context.Set<Dictionary<string, object>>("CaseLawyer")
                .Where(cl => EF.Property<Guid>(cl, "LawyerId") == lawyerId)
                .Select(cl => EF.Property<Guid>(cl, "CaseId"))
                .ToListAsync(cancellationToken);
    }
}
