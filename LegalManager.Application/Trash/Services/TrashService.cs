using LegalManager.Application.Common.Interfaces;
using LegalManager.Application.Trash.DTOs;
using LegalManager.Application.Trash.Interfaces;
using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;
using LegalManager.Application.Common.Exceptions;

namespace LegalManager.Application.Trash.Services
{
    // Restore-only archive of deactivated (baja) items; the system has no physical
    // deletion. Restore-into-occupied-slot conflicts surface as InvalidOperationException
    // from the repositories' SaveAsync unique-index translation.
    public class TrashService(
        ICaseRepository casesRepository,
        IAppointmentRepository appointmentsRepository,
        IDocumentRepository documentsRepository,
        IUserRepository usersRepository,
        ICurrentUser currentUser) : ITrashService
    {
        public async Task<TrashResponse> GetTrashAsync(CancellationToken cancellationToken = default)
        {
            if (currentUser.IsAdmin)
                return await BuildAdminTrashAsync(cancellationToken);

            return await BuildLawyerTrashAsync(cancellationToken);
        }

        public async Task<bool> RestoreCaseAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var caseItem = await casesRepository.GetByIdIncludingInactiveAsync(id, cancellationToken);
            if (caseItem == null) return false;

            await EnsureOwnsCaseAsync(caseItem, cancellationToken);

            if (caseItem.Active)
                throw new InvalidOperationException("El elemento no está en la papelera.");

            caseItem.Reactivate();
            await casesRepository.SaveAsync(cancellationToken);
            return true;
        }

        public async Task<bool> RestoreAppointmentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var appointment = await appointmentsRepository.GetByIdIncludingInactiveAsync(id, cancellationToken);
            if (appointment == null) return false;

            EnsureOwnsAppointment(appointment);

            if (appointment.Active)
                throw new InvalidOperationException("El elemento no está en la papelera.");

            appointment.Reactivate();
            await appointmentsRepository.SaveAsync(cancellationToken);
            return true;
        }

        public async Task<bool> RestoreDocumentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var document = await documentsRepository.GetByIdIncludingInactiveAsync(id, cancellationToken);
            if (document == null) return false;

            await EnsureOwnsDocumentAsync(document, cancellationToken);

            if (document.Active)
                throw new InvalidOperationException("El elemento no está en la papelera.");

            document.Reactivate();
            await documentsRepository.SaveAsync(cancellationToken);
            return true;
        }

        public async Task<bool> RestoreUserAsync(Guid id, CancellationToken cancellationToken = default)
        {
            // User restoration is admin-only even though the route admits lawyers.
            if (!currentUser.IsAdmin)
                throw new ForbiddenException("La papelera de usuarios es exclusiva del administrador.");

            var user = await usersRepository.GetByIdIncludingInactiveAsync(id, cancellationToken);
            if (user == null) return false;

            if (user.Active)
                throw new InvalidOperationException("El usuario no está en la papelera.");

            user.Reactivate();
            await usersRepository.SaveAsync(cancellationToken);
            return true;
        }

        private async Task<TrashResponse> BuildAdminTrashAsync(CancellationToken cancellationToken)
            => await BuildTrashResponseAsync(
                await casesRepository.GetInactiveAsync(cancellationToken),
                await appointmentsRepository.GetInactiveAsync(cancellationToken),
                await documentsRepository.GetInactiveAsync(cancellationToken),
                await usersRepository.GetInactiveAsync(cancellationToken),
                cancellationToken);

        // Lawyers see only their own inactive items; the users bucket stays empty
        // because user restoration is admin-only.
        private async Task<TrashResponse> BuildLawyerTrashAsync(CancellationToken cancellationToken)
        {
            var lawyerId = currentUser.Id;

            var lawyerCaseIds = (await casesRepository.GetCaseIdsForLawyerAsync(lawyerId, cancellationToken)).ToHashSet();

            return await BuildTrashResponseAsync(
                (await casesRepository.GetInactiveAsync(cancellationToken)).Where(c => lawyerCaseIds.Contains(c.Id)).ToList(),
                (await appointmentsRepository.GetInactiveAsync(cancellationToken)).Where(a => a.LawyerId == lawyerId).ToList(),
                (await documentsRepository.GetInactiveAsync(cancellationToken)).Where(d => lawyerCaseIds.Contains(d.CaseId)).ToList(),
                [],
                cancellationToken);
        }

        // Document parent labels resolve only against ACTIVE cases, so an inactive
        // parent case falls back to a null label.
        private async Task<TrashResponse> BuildTrashResponseAsync(
            IReadOnlyList<Case> cases,
            IReadOnlyList<Appointment> appointments,
            IReadOnlyList<Document> documents,
            IReadOnlyList<User> users,
            CancellationToken cancellationToken)
        {
            var activeCases = (await casesRepository.GetAllAsync(cancellationToken))
                .ToDictionary(c => c.Id, c => (CaseNumber: (string?)c.CaseNumber, Title: (string?)c.Title));

            var caseItems = new List<TrashCaseResponse>();
            foreach (var caseItem in cases)
            {
                var totalAppointments = await casesRepository.CountAppointmentsForCaseAsync(caseItem.Id, cancellationToken);
                var totalDocuments = await casesRepository.CountDocumentsForCaseAsync(caseItem.Id, cancellationToken);
                caseItems.Add(TrashCaseResponse.Desde(caseItem, totalAppointments, totalDocuments));
            }

            var appointmentItems = appointments
                .Select(TrashAppointmentResponse.Desde)
                .ToList();

            var documentItems = documents
                .Select(d =>
                {
                    var parentCase = activeCases.GetValueOrDefault(d.CaseId);
                    return TrashDocumentResponse.Desde(d, parentCase.CaseNumber, parentCase.Title);
                })
                .ToList();

            var userItems = users
                .Select(TrashUserResponse.Desde)
                .ToList();

            return new TrashResponse(caseItems, appointmentItems, documentItems, userItems);
        }

        private async Task EnsureOwnsCaseAsync(Case caseItem, CancellationToken cancellationToken)
        {
            if (currentUser.IsAdmin) return;

            var lawyerCaseIds = (await casesRepository.GetCaseIdsForLawyerAsync(currentUser.Id, cancellationToken)).ToHashSet();
            if (!lawyerCaseIds.Contains(caseItem.Id))
                throw new ForbiddenException("El elemento no pertenece a tus turnos o expedientes.");
        }

        private void EnsureOwnsAppointment(Appointment appointment)
        {
            if (currentUser.IsAdmin) return;

            if (appointment.LawyerId != currentUser.Id)
                throw new ForbiddenException("El elemento no pertenece a tus turnos o expedientes.");
        }

        private async Task EnsureOwnsDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            if (currentUser.IsAdmin) return;

            var lawyerCaseIds = (await casesRepository.GetCaseIdsForLawyerAsync(currentUser.Id, cancellationToken)).ToHashSet();
            if (!lawyerCaseIds.Contains(document.CaseId))
                throw new ForbiddenException("El elemento no pertenece a tus turnos o expedientes.");
        }
    }
}
