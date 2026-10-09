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

            // Dni y Email son los dos datos de contacto directo de la persona. Para quien no
            // sea el admin o el propio usuario no viajan: el canal de contacto es el telefono.
            return new(user.Id, user.FirstName, user.LastName, user.FullName,
                includePrivateData ? user.Dni : null, includePrivateData ? user.Email : null,
                user.RegistrationDate, type, phone, address);
        }
    }
}