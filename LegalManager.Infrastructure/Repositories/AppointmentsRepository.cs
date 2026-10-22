using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LegalManager.Infrastructure.Repositories
{
    public class AppointmentsRepository(LegalManagerDbContext context) : IAppointmentRepository
    {
        public void Add(Appointment appointment) => context.Appointments.Add(appointment);

        public async Task<IReadOnlyList<Appointment>> GetAllAsync(CancellationToken cancellationToken = default)
            => await context.Appointments.Where(a => a.Active).ToListAsync(cancellationToken);

        public async Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await context.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.Active, cancellationToken);

        public async Task<bool> HasScheduleConflictAsync(Guid lawyerId, DateOnly date, TimeOnly startTime, TimeOnly endTime, Guid? excludingAppointmentId = null, CancellationToken cancellationToken = default)
            => await context.Appointments.AnyAsync(a => a.LawyerId == lawyerId
                && a.Active
                && a.Status != AppointmentStatus.Cancelado
                && a.Date == date
                && a.Time < endTime
                && a.EndTime > startTime
                && (excludingAppointmentId == null || a.Id != excludingAppointmentId), cancellationToken);

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // The UX_Appointments_Lawyer_Slot unique index fired: two simultaneous requests
                // grabbed the same slot. It is mapped to the same InvalidOperationException the
                // in-memory check produces, so the controller still answers 409.
                throw new InvalidOperationException("El abogado ya tiene un turno en ese horario.");
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
            => ex.InnerException is SqlException { Number: 2601 or 2627 };
    }
}