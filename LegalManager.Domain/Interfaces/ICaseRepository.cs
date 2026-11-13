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

        Task<IReadOnlyDictionary<Guid, int>> CountAppointmentsByCaseIdsAsync(IReadOnlyCollection<Guid> caseIds, CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<Guid, int>> CountDocumentsByCaseIdsAsync(IReadOnlyCollection<Guid> caseIds, CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<Guid, (string CaseNumber, string Title)>> GetCaseHeadersByIdsAsync(IReadOnlyCollection<Guid> caseIds, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Guid>> GetCaseIdsForLawyerAsync(Guid lawyerId, CancellationToken cancellationToken = default);
    }
}
