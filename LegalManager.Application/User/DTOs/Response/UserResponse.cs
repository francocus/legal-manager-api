using LegalManager.Domain.Entities;

namespace LegalManager.Application.DTOs
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

            // Dni and Email are the person's two direct contact fields. For anyone other
            // than the admin or the user themselves they are omitted: the contact channel
            // is the phone.
            return new(user.Id, user.FirstName, user.LastName, user.FullName,
                includePrivateData ? user.Dni : null, includePrivateData ? user.Email : null,
                user.RegistrationDate, type, phone, address);
        }
    }
}