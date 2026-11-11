using LegalManager.Domain.Entities;

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
    }
}
