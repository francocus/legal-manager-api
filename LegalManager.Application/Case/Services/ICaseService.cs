using LegalManager.Application.DTOs;

namespace LegalManager.Application.Interfaces
{
    public interface ICaseService
    {
        Task<CaseResponse> CreateAsync(CreateCaseRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CaseResponse>> GetAllAsync(CancellationToken cancellationToken = default);

        // Paged listing. With both params null this is never called: controllers keep the
        // plain-array behavior. With either param present, page defaults to 1 and pageSize
        // to 20; page/pageSize bounds are validated here (ArgumentException -> 400).
        Task<PagedResponse<CaseResponse>> GetPagedAsync(int? page, int? pageSize, CancellationToken cancellationToken = default);

        Task<CaseResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CaseResponse?> UpdateAsync(Guid id, UpdateCaseRequest request, CancellationToken cancellationToken = default);

        Task<CaseResponse?> ChangeStatusAsync(Guid id, ChangeStatusRequest request, CancellationToken cancellationToken = default);

        Task<CaseResponse?> AddLawyerAsync(Guid id, AddLawyerRequest request, CancellationToken cancellationToken = default);

        Task<CaseResponse?> RemoveLawyerAsync(Guid id, RemoveLawyerRequest request, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}