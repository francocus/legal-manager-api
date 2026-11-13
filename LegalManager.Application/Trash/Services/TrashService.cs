using LegalManager.Application.Common.Interfaces;
using LegalManager.Application.Trash.DTOs;
using LegalManager.Application.Trash.Interfaces;
using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;
using LegalManager.Application.Common.Exceptions;

namespace LegalManager.Application.Trash.Services
{
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

        private async Task<TrashResponse> BuildTrashResponseAsync(
            IReadOnlyList<Case> cases,
            IReadOnlyList<Appointment> appointments,
            IReadOnlyList<Document> documents,
            IReadOnlyList<User> users,
            CancellationToken cancellationToken)
        {
            var caseIds = cases.Select(c => c.Id).ToList();
            var appointmentsCountMap = await casesRepository.CountAppointmentsByCaseIdsAsync(caseIds, cancellationToken);
            var documentsCountMap = await casesRepository.CountDocumentsByCaseIdsAsync(caseIds, cancellationToken);

            var caseIdsForDocs = documents.Select(d => d.CaseId).Distinct().ToList();
            var activeCases = await casesRepository.GetCaseHeadersByIdsAsync(caseIdsForDocs, cancellationToken);

            var caseItems = cases.Select(caseItem =>
            {
                var totalAppointments = appointmentsCountMap.GetValueOrDefault(caseItem.Id, 0);
                var totalDocuments = documentsCountMap.GetValueOrDefault(caseItem.Id, 0);
                return TrashCaseResponse.Desde(caseItem, totalAppointments, totalDocuments);
            }).ToList();

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
