namespace LegalManager.Application.Appointments.DTOs
{
    public record RescheduleAppointmentRequest(DateOnly Date, TimeOnly Time, TimeOnly EndTime);
}