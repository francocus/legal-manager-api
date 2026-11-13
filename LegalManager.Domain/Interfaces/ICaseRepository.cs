using LegalManager.Domain.Entities;
using LegalManager.Domain.Enums;

namespace LegalManager.Domain.Interfaces
{
    public interface ICaseRepository : IRepository<Case>
    {
        Task<(IReadOnlyList<Case> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<bool> HasAnyCaseAsClientAsync(Guid clientId, CancellationToken cancellationToken = default);

        Task<bool> HasAnyCaseAsLawyerAsync(Guid lawyerId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Case>> GetInactiveAsync(CancellationToken cancellationToken = default);

        Task<Case?> GetByIdIncludingInactiveAsync(Guid id, CancellationToken cancellationToken = default);

        Task<int> CountAppointmentsForCaseAsync(Guid caseId, CancellationToken cancellationToken = default);

        Task<int> CountDocumentsForCaseAsync(Guid caseId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Guid>> GetCaseIdsForLawyerAsync(Guid lawyerId, CancellationToken cancellationToken = default);
    }
}