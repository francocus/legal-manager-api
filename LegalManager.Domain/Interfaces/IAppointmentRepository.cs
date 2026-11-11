using LegalManager.Domain.Entities;

namespace LegalManager.Domain.Interfaces
{
    public interface IAppointmentRepository : IRepository<Appointment>
    {
        Task<bool> HasScheduleConflictAsync(Guid lawyerId, DateOnly date, TimeOnly startTime, TimeOnly endTime, Guid? excludingAppointmentId = null, CancellationToken cancellationToken = default);

        // Paged variant of GetAllAsync: same Active filter, stable Id order,
        // plus the total count of the unfiltered-by-page set. Callers must pass
        // page >= 1 and 1 <= pageSize <= 100 (validated in the application layer).
        Task<(IReadOnlyList<Appointment> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        // Role-change pre-checks: an appointment in ANY state counts, because the LawyerId
        // and ClientId FKs are Restrict.
        Task<bool> HasAnyAppointmentAsClientAsync(Guid clientId, CancellationToken cancellationToken = default);

        Task<bool> HasAnyAppointmentAsLawyerAsync(Guid lawyerId, CancellationToken cancellationToken = default);
    }
}
