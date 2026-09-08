namespace LegalManager.Application.DTOs
{
    public record UserResponse(Guid Id, string FirstName, string LastName, string Dni, string Email, DateOnly RegistrationDate, string Type, string? Phone, string? Address);
}