using LegalManager.Application.DTOs;
using LegalManager.Application.Interfaces;
using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;

namespace LegalManager.Application.Services
{
    public class AppointmentService(
        IAppointmentRepository appointmentsRepository,
        IUserRepository usersRepository,
        ICaseRepository casesRepository,
        ICurrentUser currentUser) : IAppointmentService
    {
        public AppointmentResponse Create(CreateAppointmentRequest request)
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

            EnsureCanCreate(request, relatedCase);

            if (appointmentsRepository.HasScheduleConflict(request.LawyerId, request.Date, request.Time))
                throw new InvalidOperationException("El abogado ya tiene un turno en ese horario.");

            var area = relatedCase != null ? relatedCase.Area : request.Area;
            var appointment = new Appointment(request.Title, request.Date, request.Time, request.EndTime, request.Reason, area, request.Location, request.Notes, request.ClientId, request.LawyerId, request.CaseId);
            appointmentsRepository.Add(appointment);
            appointmentsRepository.Save();
            return AppointmentResponse.Desde(appointment);
        }

        public IReadOnlyList<AppointmentResponse> GetAll()
        {
            var appointments = appointmentsRepository.GetAll();

            if (currentUser.IsAdmin)
                return appointments.Select(AppointmentResponse.Desde).ToList();

            if (currentUser.IsLawyer)
                return appointments.Where(a => a.LawyerId == currentUser.Id).Select(AppointmentResponse.Desde).ToList();

            return appointments.Where(a => a.ClientId == currentUser.Id).Select(AppointmentResponse.Desde).ToList();
        }

        public IReadOnlyList<TimeOnly> GetAvailability(Guid lawyerId, DateOnly date)
        {
            if (usersRepository.GetById(lawyerId) is not Lawyer)
                throw new ArgumentException("El abogado indicado no es válido.");

            return Appointment.ValidSlots
                .Where(slot => !appointmentsRepository.HasScheduleConflict(lawyerId, date, slot))
                .ToList();
        }

        public AppointmentResponse? GetById(Guid id)
        {
            var appointment = appointmentsRepository.GetById(id);
            if (appointment == null) return null;

            EnsureCanAccess(appointment);

            return AppointmentResponse.Desde(appointment);
        }

        public AppointmentResponse? Confirm(Guid id)
        {
            var appointment = appointmentsRepository.GetById(id);
            if (appointment == null) return null;

            EnsureCanAccess(appointment);

            appointment.Confirm();
            appointmentsRepository.Save();
            return AppointmentResponse.Desde(appointment);
        }

        public AppointmentResponse? Cancel(Guid id)
        {
            var appointment = appointmentsRepository.GetById(id);
            if (appointment == null) return null;

            EnsureCanAccess(appointment);

            appointment.Cancel();
            appointmentsRepository.Save();
            return AppointmentResponse.Desde(appointment);
        }

        public AppointmentResponse? Reschedule(Guid id, RescheduleAppointmentRequest request)
        {
            var appointment = appointmentsRepository.GetById(id);
            if (appointment == null) return null;

            EnsureCanAccess(appointment);

            if (appointmentsRepository.HasScheduleConflict(appointment.LawyerId, request.Date, request.Time))
                throw new InvalidOperationException("El abogado ya tiene un turno en ese horario.");

            appointment.Reschedule(request.Date, request.Time, request.EndTime);
            appointmentsRepository.Save();
            return AppointmentResponse.Desde(appointment);
        }

        public bool Delete(Guid id)
        {
            var appointment = appointmentsRepository.GetById(id);
            if (appointment == null) return false;

            appointment.Deactivate();
            appointmentsRepository.Save();
            return true;
        }

        private void EnsureCanCreate(CreateAppointmentRequest request, Case? relatedCase)
        {
            if (currentUser.IsAdmin) return;

            if (currentUser.IsClient)
            {
                if (request.ClientId != currentUser.Id)
                    throw new ForbiddenException("Un cliente solo puede pedir turnos a su propio nombre.");

                if (relatedCase != null && relatedCase.ClientId != currentUser.Id)
                    throw new ForbiddenException("No tiene acceso al expediente indicado.");

                return;
            }

            if (currentUser.IsLawyer)
            {
                if (request.LawyerId != currentUser.Id)
                    throw new ForbiddenException("Un abogado solo puede agendar turnos a su propio nombre.");

                if (!IsClientLinked(request.ClientId, relatedCase))
                    throw new ForbiddenException("El abogado solo puede agendar turnos para clientes vinculados a él.");

                return;
            }

            throw new ForbiddenException("No tiene permisos para crear turnos.");
        }

        private bool IsClientLinked(Guid clientId, Case? relatedCase)
        {
            if (relatedCase != null && relatedCase.LawyerIds.Contains(currentUser.Id))
                return true;

            if (casesRepository.GetAll().Any(c => c.ClientId == clientId && c.LawyerIds.Contains(currentUser.Id)))
                return true;

            return appointmentsRepository.GetAll().Any(a => a.ClientId == clientId && a.LawyerId == currentUser.Id);
        }

        private void EnsureCanAccess(Appointment appointment)
        {
            if (currentUser.IsAdmin) return;

            if (currentUser.IsLawyer && appointment.LawyerId == currentUser.Id) return;

            if (currentUser.IsClient && appointment.ClientId == currentUser.Id) return;

            throw new ForbiddenException("No tiene acceso a este turno.");
        }
    }
}