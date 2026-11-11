using LegalManager.Application.Auth.DTOs;
using LegalManager.Application.Users.DTOs;
using LegalManager.Application.Cases.DTOs;
using LegalManager.Application.Appointments.DTOs;
using LegalManager.Application.Documents.DTOs;
using LegalManager.Application.Common;

namespace LegalManager.Application.Cases.Interfaces
{
    public interface ICaseService
    {
        Task<CaseResponse> CreateAsync(CreateCaseRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CaseResponse>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<PagedResponse<CaseResponse>> GetPagedAsync(int? page, int? pageSize, CancellationToken cancellationToken = default);

        Task<CaseResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CaseResponse?> UpdateAsync(Guid id, UpdateCaseRequest request, CancellationToken cancellationToken = default);

        Task<CaseResponse?> ChangeStatusAsync(Guid id, ChangeStatusRequest request, CancellationToken cancellationToken = default);

        Task<CaseResponse?> AddLawyerAsync(Guid id, AddLawyerRequest request, CancellationToken cancellationToken = default);

        Task<CaseResponse?> RemoveLawyerAsync(Guid id, RemoveLawyerRequest request, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}