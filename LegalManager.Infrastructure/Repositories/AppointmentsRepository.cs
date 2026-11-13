using LegalManager.Domain.Entities;
using LegalManager.Domain.Enums;
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

        public async Task<bool> HasAnyAppointmentAsClientAsync(Guid clientId, CancellationToken cancellationToken = default)
            => await _dbSet.AnyAsync(a => a.ClientId == clientId, cancellationToken);

        public async Task<bool> HasAnyAppointmentAsLawyerAsync(Guid lawyerId, CancellationToken cancellationToken = default)
            => await _dbSet.AnyAsync(a => a.LawyerId == lawyerId, cancellationToken);

        public override async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await base.SaveAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                throw new InvalidOperationException("El abogado ya tiene un turno en ese horario.");
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
            => ex.InnerException is SqlException { Number: 2601 or 2627 };

        public async Task<(IReadOnlyList<Appointment> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(a => a.Active).OrderBy(a => a.Id);

            var total = await query.CountAsync(cancellationToken);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task<IReadOnlyList<Appointment>> GetInactiveAsync(CancellationToken cancellationToken = default)
            => await _dbSet.Where(a => !a.Active)
                .OrderByDescending(a => a.DeactivatedAt == null)
                .ThenByDescending(a => a.DeactivatedAt)
                .ToListAsync(cancellationToken);

        public async Task<Appointment?> GetByIdIncludingInactiveAsync(Guid id, CancellationToken cancellationToken = default)
            => await _dbSet.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }
}