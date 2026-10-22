using LegalManager.Domain.Entities;

namespace LegalManager.Domain.Interfaces
{
    public interface IUserRepository
    {
        void Add(User user);

        Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task SaveAsync(CancellationToken cancellationToken = default);
    }
}