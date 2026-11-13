using LegalManager.Application.Auth.DTOs;
using LegalManager.Application.Users.DTOs;
using LegalManager.Application.Cases.DTOs;
using LegalManager.Application.Appointments.DTOs;
using LegalManager.Application.Documents.DTOs;
using LegalManager.Application.Common;

namespace LegalManager.Application.Users.Interfaces
{
    public interface IUserService
    {
        Task<UserResponse> CreateClientAsync(CreateClientRequest request, CancellationToken cancellationToken = default);

        Task<UserResponse> CreateLawyerAsync(CreateLawyerRequest request, CancellationToken cancellationToken = default);

        Task<UserResponse> CreateAdminAsync(CreateAdminRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UserResponse>> GetClientsAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UserResponse>> GetLawyersAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UserResponse>> GetAdminsAsync(CancellationToken cancellationToken = default);

        Task<PagedResponse<UserResponse>> GetPagedAsync(int? page, int? pageSize, CancellationToken cancellationToken = default);

        Task<PagedResponse<UserResponse>> GetClientsPagedAsync(int? page, int? pageSize, CancellationToken cancellationToken = default);

        Task<PagedResponse<UserResponse>> GetLawyersPagedAsync(int? page, int? pageSize, CancellationToken cancellationToken = default);

        Task<PagedResponse<UserResponse>> GetAdminsPagedAsync(int? page, int? pageSize, CancellationToken cancellationToken = default);

        Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<UserResponse?> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);

        Task<UserResponse?> UpdateClientPhoneAsync(Guid id, string? phone, CancellationToken cancellationToken = default);

        Task<UserResponse?> UpdateLawyerPhoneAsync(Guid id, string? phone, CancellationToken cancellationToken = default);

        Task<UserResponse?> UpdateClientAddressAsync(Guid id, string? address, CancellationToken cancellationToken = default);

        Task<UserResponse?> UpdateBarNumberAsync(Guid id, string barNumber, CancellationToken cancellationToken = default);

        Task<UserResponse?> UpdateSpecialtiesAsync(Guid id, IEnumerable<string> specialties, CancellationToken cancellationToken = default);

        Task<UserResponse?> UpdateLawyerScheduleAsync(Guid id, UpdateLawyerScheduleRequest request, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        Task<UserResponse?> ConfirmEmailByAdminAsync(Guid id, CancellationToken cancellationToken = default);

        Task<UserResponse?> ChangeRoleAsync(Guid id, RoleChangeRequest request, CancellationToken cancellationToken = default);
    }
}
