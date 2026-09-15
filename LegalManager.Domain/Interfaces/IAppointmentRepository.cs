using LegalManager.Domain.Entities;

namespace LegalManager.Domain.Interfaces
{
    public interface IAppointmentRepository
    {
        void Add(Appointment appointment);

        IReadOnlyList<Appointment> GetAll();

        Appointment? GetById(Guid id);

        bool HasScheduleConflict(Guid lawyerId, DateOnly date, TimeOnly time);

        void Save();
    }
}