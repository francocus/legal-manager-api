using LegalManager.Domain.Entities;

namespace LegalManager.Domain.Interfaces
{
    public interface IAppointmentRepository
    {
        void Add(Appointment appointment);

        Task<IReadOnlyList<Appointment>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<bool> HasScheduleConflictAsync(Guid lawyerId, DateOnly date, TimeOnly startTime, TimeOnly endTime, Guid? excludingAppointmentId = null, CancellationToken cancellationToken = default);

        Task SaveAsync(CancellationToken cancellationToken = default);
    }
}