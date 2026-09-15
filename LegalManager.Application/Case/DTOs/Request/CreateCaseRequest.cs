namespace LegalManager.Application.DTOs
{
    public record CreateCaseRequest(string CaseNumber, string Title, string Area, DateOnly StartDate, string? Description, string? Notes, Guid ClientId, Guid LawyerId, Guid CreatedByUserId);
}