using LegalManager.Domain.Entities;

namespace LegalManager.Application.DTOs
{
    public record CaseResponse(
        Guid Id, string CaseNumber, string Title, string Area, CaseStatus Status,
        DateOnly StartDate, DateOnly LastUpdate, DateOnly? ClosingDate,
        string? Description, string? Notes, Guid ClientId, Guid CreatedByUserId,
        IReadOnlyList<Guid> LawyerIds, bool Active);
}