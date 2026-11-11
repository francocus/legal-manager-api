using LegalManager.Domain.Entities;
using LegalManager.Domain.Enums;

namespace LegalManager.Domain.Interfaces
{
    public interface IAppointmentRepository : IRepository<Appointment>
    {
        Task<bool> HasScheduleConflictAsync(Guid lawyerId, DateOnly date, TimeOnly startTime, TimeOnly endTime, Guid? excludingAppointmentId = null, CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<Appointment> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<bool> HasAnyAppointmentAsClientAsync(Guid clientId, CancellationToken cancellationToken = default);

        Task<bool> HasAnyAppointmentAsLawyerAsync(Guid lawyerId, CancellationToken cancellationToken = default);
    }
}