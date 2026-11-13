using LegalManager.Domain.Entities;
using LegalManager.Domain.Enums;

namespace LegalManager.Application.Users.DTOs
{
    public record UserResponse(Guid Id, string FirstName, string LastName, string FullName, string? Dni, string? Email, DateOnly RegistrationDate, string Type, string? Phone, string? Address, string? BarNumber = null, IReadOnlyList<string>? Specialties = null, string? Schedule = null)
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
            var barNumber = (user as Lawyer)?.BarNumber;
            var specialties = (user as Lawyer)?.Specialties;
            var schedule = (user as Lawyer)?.Schedule;

            return new(user.Id, user.FirstName, user.LastName, user.FullName,
                includePrivateData ? user.Dni : null, includePrivateData ? user.Email : null,
                user.RegistrationDate, type, phone, address, barNumber, specialties, schedule);
        }
    }
}
