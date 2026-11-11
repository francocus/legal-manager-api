using LegalManager.Domain.Entities;
using LegalManager.Domain.Enums;

namespace LegalManager.Application.Users.DTOs
{
    public record UserResponse(Guid Id, string FirstName, string LastName, string FullName, string? Dni, string? Email, DateOnly RegistrationDate, string Type, string? Phone, string? Address)
    {
        public static UserResponse Desde(User user, bool includePrivateData)
        {
            var type = user switch
            {
                Client => UserType.Client,
                Lawyer => UserType.Lawyer,
                Admin => UserType.Admin,
                _ => "unknown"
            };
            var phone = user switch { Client c => c.Phone, Lawyer l => l.Phone, _ => null };
            var address = (user as Client)?.Address;

            return new(user.Id, user.FirstName, user.LastName, user.FullName,
                includePrivateData ? user.Dni : null, includePrivateData ? user.Email : null,
                user.RegistrationDate, type, phone, address);
        }
    }
}