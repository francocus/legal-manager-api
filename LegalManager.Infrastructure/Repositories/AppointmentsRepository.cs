using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.Persistence;

namespace LegalManager.Infrastructure.Repositories
{
    public class AppointmentsRepository : IAppointmentRepository
    {
        private readonly LegalManagerDbContext context;

        public AppointmentsRepository(LegalManagerDbContext context)
        {
            this.context = context;
        }

        public void Add(Appointment appointment) => context.Appointments.Add(appointment);

        public IReadOnlyList<Appointment> GetAll()
            => context.Appointments.Where(a => a.Active).ToList();

        public Appointment? GetById(Guid id)
            => context.Appointments.FirstOrDefault(a => a.Id == id && a.Active);

        public bool HasScheduleConflict(Guid lawyerId, DateOnly date, TimeOnly time, TimeOnly endTime)
            => context.Appointments.Any(a => a.LawyerId == lawyerId
                && a.Active
                && a.Status != AppointmentStatus.Cancelado
                && a.Date == date
                && a.Time == time);

        public void Save() => context.SaveChanges();
    }
}