using LegalManager.Domain.Entities;

namespace LegalManager.Domain.Interfaces
{
    public interface ICaseRepository : IRepository<Case>
    {
        // Paged variant of GetAllAsync: same Active filter, stable Id order,
        // plus the total count of the unfiltered-by-page set. Callers must pass
        // page >= 1 and 1 <= pageSize <= 100 (validated in the application layer).
        Task<(IReadOnlyList<Case> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    }
}
