using LegalManager.Domain.Entities;

namespace LegalManager.Domain.Interfaces
{
    public interface IAppointmentRepository : IRepository<Appointment>
    {
        Task<bool> HasScheduleConflictAsync(Guid lawyerId, DateOnly date, TimeOnly startTime, TimeOnly endTime, Guid? excludingAppointmentId = null, CancellationToken cancellationToken = default);
    }
}
