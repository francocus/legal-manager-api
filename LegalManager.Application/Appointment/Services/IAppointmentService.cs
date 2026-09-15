using LegalManager.Application.DTOs;

namespace LegalManager.Application.Interfaces
{
    public interface IAppointmentService
    {
        AppointmentResponse Create(CreateAppointmentRequest request);

        IReadOnlyList<AppointmentResponse> GetAll();

        IReadOnlyList<TimeOnly> GetAvailability(Guid lawyerId, DateOnly date);

        AppointmentResponse? GetById(Guid id);

        AppointmentResponse? Confirm(Guid id);

        AppointmentResponse? Cancel(Guid id);

        AppointmentResponse? Reschedule(Guid id, RescheduleAppointmentRequest request);

        bool Delete(Guid id);
    }
}