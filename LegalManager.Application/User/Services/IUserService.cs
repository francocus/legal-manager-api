using LegalManager.Application.DTOs;
using LegalManager.Domain.Entities;

namespace LegalManager.Application.Interfaces
{
    public interface IUserService
    {
        Client CreateClient(CreateClientRequest request);

        Lawyer CreateLawyer(CreateLawyerRequest request);

        Admin CreateAdmin(CreateAdminRequest request);

        IReadOnlyList<User> GetAll();

        IReadOnlyList<User> GetClients();

        IReadOnlyList<User> GetLawyers();

        IReadOnlyList<User> GetAdmins();

        User? GetById(Guid id);

        User? Update(Guid id, UpdateUserRequest request);

        User? UpdateClientPhone(Guid id, string? phone);

        User? UpdateLawyerPhone(Guid id, string? phone);

        User? UpdateClientAddress(Guid id, string? address);

        User? UpdateBarNumber(Guid id, string barNumber);

        User? UpdateSpecialties(Guid id, IEnumerable<string> specialties);

        bool Delete(Guid id);
    }
}