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

        Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<UserResponse?> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);

        Task<UserResponse?> UpdateClientPhoneAsync(Guid id, string? phone, CancellationToken cancellationToken = default);

        Task<UserResponse?> UpdateLawyerPhoneAsync(Guid id, string? phone, CancellationToken cancellationToken = default);

        Task<UserResponse?> UpdateClientAddressAsync(Guid id, string? address, CancellationToken cancellationToken = default);

        Task<UserResponse?> UpdateBarNumberAsync(Guid id, string barNumber, CancellationToken cancellationToken = default);

        Task<UserResponse?> UpdateSpecialtiesAsync(Guid id, IEnumerable<string> specialties, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}