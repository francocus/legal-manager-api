using LegalManager.Application.DTOs;
using LegalManager.Application.Interfaces;
using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;

namespace LegalManager.Application.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository appointmentsRepository;
        private readonly IUserRepository usersRepository;
        private readonly ICaseRepository casesRepository;

        public AppointmentService(IAppointmentRepository appointmentsRepository, IUserRepository usersRepository, ICaseRepository casesRepository)
        {
            this.appointmentsRepository = appointmentsRepository;
            this.usersRepository = usersRepository;
            this.casesRepository = casesRepository;
        }

        public Appointment Create(CreateAppointmentRequest request)
        {
            if (usersRepository.GetById(request.ClientId) is not Client)
                throw new ArgumentException("El cliente indicado no es válido.");

            if (usersRepository.GetById(request.LawyerId) is not Lawyer)
                throw new ArgumentException("El abogado indicado no es válido.");

            Case? relatedCase = null;
            if (request.CaseId.HasValue)
            {
                relatedCase = casesRepository.GetById(request.CaseId.Value);
                if (relatedCase == null)
                    throw new ArgumentException("El expediente indicado no existe.");
            }

            if (appointmentsRepository.HasScheduleConflict(request.LawyerId, request.Date, request.Time, request.EndTime))
                throw new InvalidOperationException("El abogado ya tiene un turno en ese horario.");

            var area = relatedCase != null ? relatedCase.Area : request.Area;
            var appointment = new Appointment(request.Title, request.Date, request.Time, request.EndTime, request.Reason, area, request.Location, request.Notes, request.ClientId, request.LawyerId, request.CaseId);
            appointmentsRepository.Add(appointment);
            appointmentsRepository.Save();
            return appointment;
        }

        public IReadOnlyList<Appointment> GetAll() => appointmentsRepository.GetAll();

        public IReadOnlyList<TimeOnly> GetAvailability(Guid lawyerId, DateOnly date)
        {
            if (usersRepository.GetById(lawyerId) is not Lawyer)
                throw new ArgumentException("El abogado indicado no es válido.");

            return Appointment.ValidSlots
                .Where(slot => !appointmentsRepository.HasScheduleConflict(lawyerId, date, slot, slot))
                .ToList();
        }

        public Appointment? GetById(Guid id) => appointmentsRepository.GetById(id);

        public Appointment? Confirm(Guid id)
        {
            var appointment = GetById(id);
            if (appointment == null) return null;

            appointment.Confirm();
            appointmentsRepository.Save();
            return appointment;
        }

        public Appointment? Cancel(Guid id)
        {
            var appointment = GetById(id);
            if (appointment == null) return null;

            appointment.Cancel();
            appointmentsRepository.Save();
            return appointment;
        }

        public Appointment? Reschedule(Guid id, RescheduleAppointmentRequest request)
        {
            var appointment = GetById(id);
            if (appointment == null) return null;

            if (appointmentsRepository.HasScheduleConflict(appointment.LawyerId, request.Date, request.Time, request.EndTime))
                throw new InvalidOperationException("El abogado ya tiene un turno en ese horario.");

            appointment.Reschedule(request.Date, request.Time, request.EndTime);
            appointmentsRepository.Save();
            return appointment;
        }

        public bool Delete(Guid id)
        {
            var appointment = GetById(id);
            if (appointment == null) return false;

            appointment.Deactivate();
            appointmentsRepository.Save();
            return true;
        }
    }
}