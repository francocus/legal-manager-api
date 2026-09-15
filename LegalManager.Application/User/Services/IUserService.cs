using LegalManager.Application.DTOs;

namespace LegalManager.Application.Interfaces
{
    public interface IUserService
    {
        UserResponse CreateClient(CreateClientRequest request);

        UserResponse CreateLawyer(CreateLawyerRequest request);

        UserResponse CreateAdmin(CreateAdminRequest request);

        IReadOnlyList<UserResponse> GetAll();

        IReadOnlyList<UserResponse> GetClients();

        IReadOnlyList<UserResponse> GetLawyers();

        IReadOnlyList<UserResponse> GetAdmins();

        UserResponse? GetById(Guid id);

        UserResponse? Update(Guid id, UpdateUserRequest request);

        UserResponse? UpdateClientPhone(Guid id, string? phone);

        UserResponse? UpdateLawyerPhone(Guid id, string? phone);

        UserResponse? UpdateClientAddress(Guid id, string? address);

        UserResponse? UpdateBarNumber(Guid id, string barNumber);

        UserResponse? UpdateSpecialties(Guid id, IEnumerable<string> specialties);

        bool Delete(Guid id);
    }
}