namespace LegalManager.Application.Appointments.DTOs
{
    public record AvailabilityResponse(IReadOnlyList<TimeOnly> Slots);
}