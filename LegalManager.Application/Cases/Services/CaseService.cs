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

namespace LegalManager.Application.Cases.Services
{
    public class CaseService(
        ICaseRepository casesRepository,
        IUserRepository usersRepository,
        IAppointmentRepository appointmentsRepository,
        ICurrentUser currentUser) : ICaseService
    {
        public async Task<CaseResponse> CreateAsync(CreateCaseRequest request, CancellationToken cancellationToken = default)
        {
            var allCases = await casesRepository.GetAllAsync(cancellationToken);
            if (allCases.Any(c => c.CaseNumber == request.CaseNumber))
                throw new InvalidOperationException("Ya existe un expediente con ese número.");

            var client = await usersRepository.GetByIdAsync(request.ClientId, cancellationToken);
            if (client is not Client)
                throw new ArgumentException("El cliente indicado no es válido.");

            var initialLawyer = await usersRepository.GetByIdAsync(request.LawyerId, cancellationToken);
            if (initialLawyer is not Lawyer)
                throw new ArgumentException("El abogado indicado no es válido.");

            if (currentUser.IsLawyer && request.LawyerId != currentUser.Id)
                throw new ForbiddenException("Un abogado solo puede crear expedientes a su propio nombre.");

            var caseItem = new Case(request.CaseNumber, request.Title, request.Area, request.StartDate, request.Description, request.Notes, request.ClientId, (Lawyer)initialLawyer, currentUser.Id);
            casesRepository.Add(caseItem);
            await casesRepository.SaveAsync(cancellationToken);
            return CaseResponse.Desde(caseItem);
        }

        public async Task<IReadOnlyList<CaseResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var cases = await casesRepository.GetAllAsync(cancellationToken);

            return FilterByRole(cases).Select(CaseResponse.Desde).ToList();
        }

        public async Task<PagedResponse<CaseResponse>> GetPagedAsync(int? page, int? pageSize, CancellationToken cancellationToken = default)
        {
            var (currentPage, currentPageSize) = PagedResponse.Normalize(page, pageSize);

            if (currentUser.IsAdmin)
            {
                var (adminItems, adminTotal) = await casesRepository.GetPagedAsync(currentPage, currentPageSize, cancellationToken);
                return new PagedResponse<CaseResponse>(adminItems.Select(CaseResponse.Desde).ToList(), currentPage, currentPageSize, adminTotal);
            }

            var visible = FilterByRole(await casesRepository.GetAllAsync(cancellationToken))
                .OrderBy(c => c.Id)
                .ToList();

            var items = visible
                .Skip((currentPage - 1) * currentPageSize)
                .Take(currentPageSize)
                .Select(CaseResponse.Desde)
                .ToList();

            return new PagedResponse<CaseResponse>(items, currentPage, currentPageSize, visible.Count);
        }

        private IReadOnlyList<Case> FilterByRole(IReadOnlyList<Case> cases)
        {
            if (currentUser.IsAdmin) return cases;

            if (currentUser.IsLawyer)
                return cases.Where(c => c.LawyerIds.Contains(currentUser.Id)).ToList();

            return cases.Where(c => c.ClientId == currentUser.Id).ToList();
        }

        public async Task<CaseResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var caseItem = await casesRepository.GetByIdAsync(id, cancellationToken);
            if (caseItem == null) return null;

            EnsureCanAccess(caseItem);

            return CaseResponse.Desde(caseItem);
        }

        public async Task<CaseResponse?> UpdateAsync(Guid id, UpdateCaseRequest request, CancellationToken cancellationToken = default)
        {
            var caseItem = await casesRepository.GetByIdAsync(id, cancellationToken);
            if (caseItem == null) return null;

            EnsureCanManage(caseItem);

            caseItem.UpdateDetails(request.Title, request.Area, request.Description, request.Notes);
            await casesRepository.SaveAsync(cancellationToken);
            return CaseResponse.Desde(caseItem);
        }

        public async Task<CaseResponse?> ChangeStatusAsync(Guid id, ChangeStatusRequest request, CancellationToken cancellationToken = default)
        {
            var caseItem = await casesRepository.GetByIdAsync(id, cancellationToken);
            if (caseItem == null) return null;

            EnsureCanManage(caseItem);

            caseItem.ChangeStatus(request.Status);
            await casesRepository.SaveAsync(cancellationToken);
            return CaseResponse.Desde(caseItem);
        }

        public async Task<CaseResponse?> AddLawyerAsync(Guid id, AddLawyerRequest request, CancellationToken cancellationToken = default)
        {
            var caseItem = await casesRepository.GetByIdAsync(id, cancellationToken);
            if (caseItem == null) return null;

            EnsureCanManage(caseItem);

            var lawyer = await usersRepository.GetByIdAsync(request.LawyerId, cancellationToken);
            if (lawyer is not Lawyer)
                throw new ArgumentException("El abogado indicado no es válido.");

            caseItem.AddLawyer((Lawyer)lawyer);
            await casesRepository.SaveAsync(cancellationToken);
            return CaseResponse.Desde(caseItem);
        }

        public async Task<CaseResponse?> RemoveLawyerAsync(Guid id, RemoveLawyerRequest request, CancellationToken cancellationToken = default)
        {
            var caseItem = await casesRepository.GetByIdAsync(id, cancellationToken);
            if (caseItem == null) return null;

            EnsureCanManage(caseItem);

            var lawyer = await usersRepository.GetByIdAsync(request.LawyerId, cancellationToken);
            if (lawyer is not Lawyer)
                throw new ArgumentException("El abogado indicado no es válido.");

            caseItem.RemoveLawyer(request.LawyerId);
            await casesRepository.SaveAsync(cancellationToken);
            return CaseResponse.Desde(caseItem);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var caseItem = await casesRepository.GetByIdAsync(id, cancellationToken);
            if (caseItem == null) return false;

            EnsureCanManage(caseItem);

            var allAppointments = await appointmentsRepository.GetAllAsync(cancellationToken);
            var hasActiveAppointments = allAppointments
                .Any(a => a.CaseId == id
                    && a.EffectiveStatus != AppointmentStatus.Cancelado && a.EffectiveStatus != AppointmentStatus.Finalizado);

            if (hasActiveAppointments)
                throw new InvalidOperationException($"El expediente con id {id} tiene turnos activos asociados.");

            caseItem.Deactivate();
            await casesRepository.SaveAsync(cancellationToken);
            return true;
        }

        private void EnsureCanManage(Case caseItem)
        {
            if (currentUser.IsAdmin) return;

            if (currentUser.IsLawyer && caseItem.LawyerIds.Contains(currentUser.Id)) return;

            throw new ForbiddenException("Solo el administrador o un abogado que gestiona el expediente pueden modificarlo.");
        }

        private void EnsureCanAccess(Case caseItem)
        {
            if (currentUser.IsAdmin) return;

            if (currentUser.IsLawyer && caseItem.LawyerIds.Contains(currentUser.Id)) return;

            if (currentUser.IsClient && caseItem.ClientId == currentUser.Id) return;

            throw new ForbiddenException("No tiene acceso a este expediente.");
        }
    }
}