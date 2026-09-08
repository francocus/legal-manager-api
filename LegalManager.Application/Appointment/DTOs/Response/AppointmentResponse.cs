using LegalManager.Domain.Entities;

namespace LegalManager.Application.DTOs
{
    public record AppointmentResponse(
        Guid Id, string Title, DateOnly Date, TimeOnly Time, TimeOnly EndTime,
        string Reason, AppointmentStatus Status, AppointmentStatus EffectiveStatus, string? Area,
        string? Location, string? Notes, Guid ClientId, Guid LawyerId,
        Guid? CaseId, bool Active);
}