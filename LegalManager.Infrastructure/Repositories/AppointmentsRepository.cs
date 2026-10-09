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

        public IReadOnlyList<Appointment> GetAll()
            => [.. context.Appointments.Where(a => a.Active)];

        public Appointment? GetById(Guid id)
            => context.Appointments.FirstOrDefault(a => a.Id == id && a.Active);

        public bool HasScheduleConflict(Guid lawyerId, DateOnly date, TimeOnly time)
            => context.Appointments.Any(a => a.LawyerId == lawyerId
                && a.Active
                && a.Status != AppointmentStatus.Cancelado
                && a.Date == date
                && a.Time == time);

        public void Save()
        {
            try
            {
                context.SaveChanges();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // El indice unico UX_Appointments_Lawyer_Slot salto: dos requests simultaneos
                // agarraron el mismo slot. Se traduce al mismo InvalidOperationException que
                // produce el chequeo en memoria, para que el controller responda 409 igual.
                throw new InvalidOperationException("El abogado ya tiene un turno en ese horario.");
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
            => ex.InnerException is SqlException { Number: 2601 or 2627 };
    }
}