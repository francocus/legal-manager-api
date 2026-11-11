namespace LegalManager.Application.DTOs
{
    public record RoleChangeRequest(string Role, string? BarNumber, string[]? Specialties);
}
