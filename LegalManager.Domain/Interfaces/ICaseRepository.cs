using LegalManager.Domain.Entities;

namespace LegalManager.Domain.Interfaces
{
    public interface ICaseRepository
    {
        void Add(Case caseItem);

        Task<IReadOnlyList<Case>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<Case?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task SaveAsync(CancellationToken cancellationToken = default);
    }
}