using LegalManager.Domain.Entities;

namespace LegalManager.Application.DTOs
{
    public record AppointmentResponse(
        Guid Id, string Title, DateOnly Date, TimeOnly Time, TimeOnly EndTime, int DurationMinutes,
        string? Reason, AppointmentStatus Status, AppointmentStatus EffectiveStatus, string? Area,
        string? Location, string? Notes, Guid ClientId, Guid LawyerId,
        Guid? CaseId, bool Active)
    {
        public static AppointmentResponse Desde(Appointment a) => new(
            a.Id, a.Title, a.Date, a.Time, a.EndTime, a.DurationMinutes, a.Reason, a.Status, a.EffectiveStatus,
            a.Area, a.Location, a.Notes, a.ClientId, a.LawyerId, a.CaseId, a.Active);
    }
}