using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LegalManager.Infrastructure.Repositories
{
    public class AppointmentsRepository(LegalManagerDbContext context) : GenericRepository<Appointment>(context), IAppointmentRepository
    {
        public async Task<bool> HasScheduleConflictAsync(Guid lawyerId, DateOnly date, TimeOnly startTime, TimeOnly endTime, Guid? excludingAppointmentId = null, CancellationToken cancellationToken = default)
            => await _dbSet.AnyAsync(a => a.LawyerId == lawyerId
                && a.Active
                && a.Status != AppointmentStatus.Cancelado
                && a.Date == date
                && a.Time < endTime
                && a.EndTime > startTime
                && (excludingAppointmentId == null || a.Id != excludingAppointmentId), cancellationToken);

        public override async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await base.SaveAsync(cancellationToken);
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
