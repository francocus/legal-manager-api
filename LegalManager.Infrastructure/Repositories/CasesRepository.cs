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

        public async Task<int> CountAppointmentsForCaseAsync(Guid caseId, CancellationToken cancellationToken = default)
            => await _context.Set<Appointment>().CountAsync(a => a.CaseId == caseId, cancellationToken);

        public async Task<int> CountDocumentsForCaseAsync(Guid caseId, CancellationToken cancellationToken = default)
            => await _context.Set<Document>().CountAsync(d => d.CaseId == caseId, cancellationToken);

        // Shared-type join: no CLR type, so rows are read through the property-bag set
        // by shadow FK name; membership must not depend on the case Active flag.
        public async Task<IReadOnlyList<Guid>> GetCaseIdsForLawyerAsync(Guid lawyerId, CancellationToken cancellationToken = default)
            => await _context.Set<Dictionary<string, object>>("CaseLawyer")
                .Where(cl => EF.Property<Guid>(cl, "LawyerId") == lawyerId)
                .Select(cl => EF.Property<Guid>(cl, "CaseId"))
                .ToListAsync(cancellationToken);
    }
}