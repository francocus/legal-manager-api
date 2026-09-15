using LegalManager.Domain.Entities;

namespace LegalManager.Application.DTOs
{
    public record CaseResponse(
        Guid Id, string CaseNumber, string Title, string Area, CaseStatus Status,
        DateOnly StartDate, DateOnly LastUpdate, DateOnly? ClosingDate,
        string? Description, string? Notes, Guid ClientId, Guid CreatedByUserId,
        IReadOnlyList<Guid> LawyerIds, bool Active)
    {
        public static CaseResponse Desde(Case c) => new(
            c.Id, c.CaseNumber, c.Title, c.Area, c.Status,
            c.StartDate, c.LastUpdate, c.ClosingDate,
            c.Description, c.Notes, c.ClientId, c.CreatedByUserId, c.LawyerIds, c.Active);
    }
}