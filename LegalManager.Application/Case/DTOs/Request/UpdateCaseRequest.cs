namespace LegalManager.Application.DTOs
{
    public record UpdateCaseRequest(string Title, string Area, string? Description, string? Notes);
}