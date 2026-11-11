using LegalManager.Application.DTOs;

namespace LegalManager.Application.Interfaces
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

        // Paged listings. With both params null these are never called: controllers keep
        // the plain-array behavior. With either param present, page defaults to 1 and
        // pageSize to 20; page/pageSize bounds are validated here (ArgumentException -> 400).
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

        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        Task<UserResponse?> ConfirmEmailByAdminAsync(Guid id, CancellationToken cancellationToken = default);

        // Admin flow: fixes the self-registered role (client <-> lawyer) keeping the same
        // user id. Admins are out of this flow: they are command-seeded and never change role.
        Task<UserResponse?> ChangeRoleAsync(Guid id, RoleChangeRequest request, CancellationToken cancellationToken = default);
    }
}