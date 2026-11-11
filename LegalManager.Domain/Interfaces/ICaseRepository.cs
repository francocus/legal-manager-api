using LegalManager.Domain.Entities;

namespace LegalManager.Domain.Interfaces
{
    public interface ICaseRepository : IRepository<Case>
    {
        // Paged variant of GetAllAsync: same Active filter, stable Id order,
        // plus the total count of the unfiltered-by-page set. Callers must pass
        // page >= 1 and 1 <= pageSize <= 100 (validated in the application layer).
        Task<(IReadOnlyList<Case> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        // Role-change pre-checks: a case in ANY state counts, because the ClientId FK on
        // Cases and the CaseLawyer join FK are Restrict.
        Task<bool> HasAnyCaseAsClientAsync(Guid clientId, CancellationToken cancellationToken = default);

        Task<bool> HasAnyCaseAsLawyerAsync(Guid lawyerId, CancellationToken cancellationToken = default);
    }
}
