using System.Text.Json;
using LegalManager.Application.Auth.DTOs;
using LegalManager.Application.Users.DTOs;
using LegalManager.Application.Cases.DTOs;
using LegalManager.Application.Appointments.DTOs;
using LegalManager.Application.Documents.DTOs;
using LegalManager.Application.Common;
using LegalManager.Application.Auth.Interfaces;
using LegalManager.Application.Users.Interfaces;
using LegalManager.Application.Cases.Interfaces;
using LegalManager.Application.Appointments.Interfaces;
using LegalManager.Application.Documents.Interfaces;
using LegalManager.Application.Common.Interfaces;
using LegalManager.Domain.Entities;
using LegalManager.Domain.Enums;
using LegalManager.Domain.Interfaces;
using LegalManager.Application.Common.Exceptions;

namespace LegalManager.Application.Appointments.Services
{
    public class AppointmentService(
        IAppointmentRepository appointmentsRepository,
        IUserRepository usersRepository,
        ICaseRepository casesRepository,
        ICurrentUser currentUser) : IAppointmentService
    {
        public async Task<AppointmentResponse> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default)
        {
            var client = await usersRepository.GetByIdAsync(request.ClientId, cancellationToken);
            if (client is not Client)
                throw new ArgumentException("El cliente indicado no es válido.");

            var lawyer = await usersRepository.GetByIdAsync(request.LawyerId, cancellationToken);
            if (lawyer is not Lawyer)
                throw new ArgumentException("El abogado indicado no es válido.");

            Case? relatedCase = null;
            if (request.CaseId.HasValue)
            {
                relatedCase = await casesRepository.GetByIdAsync(request.CaseId.Value, cancellationToken);
                if (relatedCase == null)
                    throw new ArgumentException("El expediente indicado no existe.");
            }

            await EnsureCanCreateAsync(request, relatedCase, cancellationToken);

            var area = relatedCase != null ? relatedCase.Area : request.Area;
            var appointment = new Appointment(request.Title, request.Date, request.Time, request.EndTime, request.Reason, area, request.Location, request.Notes, request.ClientId, request.LawyerId, request.CaseId);

            if (await appointmentsRepository.HasScheduleConflictAsync(request.LawyerId, request.Date, request.Time, request.EndTime, null, cancellationToken))
                throw new InvalidOperationException("El abogado ya tiene un turno en ese horario.");

            appointmentsRepository.Add(appointment);
            await appointmentsRepository.SaveAsync(cancellationToken);
            return AppointmentResponse.Desde(appointment);
        }

        public async Task<IReadOnlyList<AppointmentResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var appointments = await appointmentsRepository.GetAllAsync(cancellationToken);

            return FilterByRole(appointments).Select(AppointmentResponse.Desde).ToList();
        }

        public async Task<PagedResponse<AppointmentResponse>> GetPagedAsync(int? page, int? pageSize, CancellationToken cancellationToken = default)
        {
            var (currentPage, currentPageSize) = PagedResponse.Normalize(page, pageSize);

            if (currentUser.IsAdmin)
            {
                var (adminItems, adminTotal) = await appointmentsRepository.GetPagedAsync(currentPage, currentPageSize, cancellationToken);
                return new PagedResponse<AppointmentResponse>(adminItems.Select(AppointmentResponse.Desde).ToList(), currentPage, currentPageSize, adminTotal);
            }

            var visible = FilterByRole(await appointmentsRepository.GetAllAsync(cancellationToken))
                .OrderBy(a => a.Id)
                .ToList();

            var items = visible
                .Skip((currentPage - 1) * currentPageSize)
                .Take(currentPageSize)
                .Select(AppointmentResponse.Desde)
                .ToList();

            return new PagedResponse<AppointmentResponse>(items, currentPage, currentPageSize, visible.Count);
        }

        private IReadOnlyList<Appointment> FilterByRole(IReadOnlyList<Appointment> appointments)
        {
            if (currentUser.IsAdmin) return appointments;

            if (currentUser.IsLawyer)
                return appointments.Where(a => a.LawyerId == currentUser.Id).ToList();

            return appointments.Where(a => a.ClientId == currentUser.Id).ToList();
        }

        public async Task<IReadOnlyList<TimeOnly>> GetAvailabilityAsync(Guid lawyerId, DateOnly date, CancellationToken cancellationToken = default)
        {
            var lawyer = await usersRepository.GetByIdAsync(lawyerId, cancellationToken);
            if (lawyer is not Lawyer lawyerEntity)
                throw new ArgumentException("El abogado indicado no es válido.");

            var workingSlots = Appointment.ValidSlots;

            if (!string.IsNullOrWhiteSpace(lawyerEntity.Schedule))
            {
                try
                {
                    using var doc = JsonDocument.Parse(lawyerEntity.Schedule);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("days", out var daysElem) && daysElem.ValueKind == JsonValueKind.Array)
                    {
                        var days = daysElem.EnumerateArray()
                            .Where(e => e.TryGetInt32(out _))
                            .Select(e => e.GetInt32())
                            .ToList();

                        var dayInt = (int)date.DayOfWeek;
                        if (!days.Contains(dayInt))
                        {
                            return Array.Empty<TimeOnly>();
                        }
                    }

                    if (root.TryGetProperty("slots", out var slotsElem) && slotsElem.ValueKind == JsonValueKind.Array)
                    {
                        var allowedSlots = new List<TimeOnly>();
                        foreach (var s in slotsElem.EnumerateArray())
                        {
                            if (TimeOnly.TryParse(s.GetString(), out var t) && Appointment.ValidSlots.Contains(t))
                            {
                                allowedSlots.Add(t);
                            }
                        }

                        if (allowedSlots.Count > 0)
                        {
                            workingSlots = allowedSlots.OrderBy(t => t).ToList().AsReadOnly();
                        }
                    }
                }
                catch
                {
                    // Fallback to default slots on format error
                }
            }

            var availableSlots = new List<TimeOnly>();
            foreach (var slot in workingSlots)
            {
                var hasConflict = await appointmentsRepository.HasScheduleConflictAsync(
                    lawyerId, date, slot, slot.AddMinutes(Appointment.SlotDurationMinutes), null, cancellationToken);

                if (!hasConflict)
                    availableSlots.Add(slot);
            }

            return availableSlots;
        }

        public async Task<AppointmentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var appointment = await appointmentsRepository.GetByIdAsync(id, cancellationToken);
            if (appointment == null) return null;

            EnsureCanAccess(appointment);

            return AppointmentResponse.Desde(appointment);
        }

        public async Task<AppointmentResponse?> ConfirmAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var appointment = await appointmentsRepository.GetByIdAsync(id, cancellationToken);
            if (appointment == null) return null;

            EnsureCanAccess(appointment);

            appointment.Confirm();
            await appointmentsRepository.SaveAsync(cancellationToken);
            return AppointmentResponse.Desde(appointment);
        }

        public async Task<AppointmentResponse?> CancelAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var appointment = await appointmentsRepository.GetByIdAsync(id, cancellationToken);
            if (appointment == null) return null;

            EnsureCanAccess(appointment);

            appointment.Cancel();
            await appointmentsRepository.SaveAsync(cancellationToken);
            return AppointmentResponse.Desde(appointment);
        }

        public async Task<AppointmentResponse?> RescheduleAsync(Guid id, RescheduleAppointmentRequest request, CancellationToken cancellationToken = default)
        {
            var appointment = await appointmentsRepository.GetByIdAsync(id, cancellationToken);
            if (appointment == null) return null;

            EnsureCanAccess(appointment);

            if (await appointmentsRepository.HasScheduleConflictAsync(appointment.LawyerId, request.Date, request.Time, request.EndTime, excludingAppointmentId: id, cancellationToken: cancellationToken))
                throw new InvalidOperationException("El abogado ya tiene un turno en ese horario.");

            appointment.Reschedule(request.Date, request.Time, request.EndTime);
            await appointmentsRepository.SaveAsync(cancellationToken);
            return AppointmentResponse.Desde(appointment);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var appointment = await appointmentsRepository.GetByIdAsync(id, cancellationToken);
            if (appointment == null) return false;

            appointment.Deactivate();
            await appointmentsRepository.SaveAsync(cancellationToken);
            return true;
        }

        private async Task EnsureCanCreateAsync(CreateAppointmentRequest request, Case? relatedCase, CancellationToken cancellationToken = default)
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

                if (!await IsClientLinkedAsync(request.ClientId, relatedCase, cancellationToken))
                    throw new ForbiddenException("El abogado solo puede agendar turnos para clientes vinculados a él.");

                return;
            }

            throw new ForbiddenException("No tiene permisos para crear turnos.");
        }

        private async Task<bool> IsClientLinkedAsync(Guid clientId, Case? relatedCase, CancellationToken cancellationToken = default)
        {
            if (relatedCase != null && relatedCase.LawyerIds.Contains(currentUser.Id))
                return true;

            var allCases = await casesRepository.GetAllAsync(cancellationToken);
            if (allCases.Any(c => c.ClientId == clientId && c.LawyerIds.Contains(currentUser.Id)))
                return true;

            var allAppointments = await appointmentsRepository.GetAllAsync(cancellationToken);
            return allAppointments.Any(a => a.ClientId == clientId && a.LawyerId == currentUser.Id);
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