using LegalManager.Domain.Entities;
using LegalManager.Domain.Enums;

namespace LegalManager.Domain.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<User?> GetByEmailConfirmationTokenAsync(string tokenHash, CancellationToken cancellationToken = default);

        Task<User?> GetByPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken = default);

        // Paged variants of GetAllAsync and the OfType getters: same Active filter over
        // the TPC union, stable Id order, plus the total count of the unfiltered-by-page
        // set. Callers must pass page >= 1 and 1 <= pageSize <= 100 (validated in the
        // application layer).
        Task<(IReadOnlyList<User> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<User> Items, int Total)> GetClientsPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<User> Items, int Total)> GetLawyersPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<User> Items, int Total)> GetAdminsPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        // Client <-> lawyer move: deletes the source TPC row and inserts the target one in a
        // single save, preserving the user's Id and every base column. The service owns the
        // pre-checks (admin target, same-role idempotency, Restrict FK guards); here the DB
        // rejection of a lost race bubbles as-is.
        Task<User?> ChangeRoleAsync(Guid id, string targetRole, string? barNumber, string? specialtiesJson, CancellationToken cancellationToken = default);
    }
}
