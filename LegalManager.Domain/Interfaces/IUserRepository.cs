using LegalManager.Domain.Entities;

namespace LegalManager.Domain.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<User?> GetByEmailConfirmationTokenAsync(string tokenHash, CancellationToken cancellationToken = default);

        Task<User?> GetByPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken = default);
    }
}
