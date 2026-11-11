namespace LegalManager.Application.Users.DTOs
{
    public record RoleChangeRequest(string Role, string? BarNumber, string[]? Specialties);
}
