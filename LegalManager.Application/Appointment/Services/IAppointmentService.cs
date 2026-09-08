using LegalManager.Application.DTOs;
using LegalManager.Domain.Entities;

namespace LegalManager.Application.Interfaces
{
    public interface IAppointmentService
    {
        Appointment Create(CreateAppointmentRequest request);

        IReadOnlyList<Appointment> GetAll();

        IReadOnlyList<TimeOnly> GetAvailability(Guid lawyerId, DateOnly date);

        Appointment? GetById(Guid id);

        Appointment? Confirm(Guid id);

        Appointment? Cancel(Guid id);

        Appointment? Reschedule(Guid id, RescheduleAppointmentRequest request);

        bool Delete(Guid id);
    }
}