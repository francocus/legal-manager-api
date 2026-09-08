using LegalManager.Application.DTOs;
using LegalManager.Application.Interfaces;
using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;

namespace LegalManager.Application.Services
{
    public class CaseService : ICaseService
    {
        private readonly ICaseRepository casesRepository;
        private readonly IUserRepository usersRepository;
        private readonly IAppointmentRepository appointmentsRepository;

        public CaseService(ICaseRepository casesRepository, IUserRepository usersRepository, IAppointmentRepository appointmentsRepository)
        {
            this.casesRepository = casesRepository;
            this.usersRepository = usersRepository;
            this.appointmentsRepository = appointmentsRepository;
        }

        public Case Create(CreateCaseRequest request)
        {
            if (casesRepository.GetAll().Any(c => c.CaseNumber == request.CaseNumber))
                throw new InvalidOperationException("Ya existe un expediente con ese número.");

            if (usersRepository.GetById(request.ClientId) is not Client)
                throw new ArgumentException("El cliente indicado no es válido.");

            var initialLawyer = usersRepository.GetById(request.LawyerId);
            if (initialLawyer is not Lawyer)
                throw new ArgumentException("El abogado indicado no es válido.");

            var createdBy = usersRepository.GetById(request.CreatedByUserId);
            if (createdBy is not Lawyer && createdBy is not Admin)
                throw new ArgumentException("El expediente solo puede ser creado por un abogado o un administrador.");

            var caseItem = new Case(request.CaseNumber, request.Title, request.Area, request.StartDate, request.Description, request.Notes, request.ClientId, (Lawyer)initialLawyer, request.CreatedByUserId);
            casesRepository.Add(caseItem);
            casesRepository.Save();
            return caseItem;
        }

        public IReadOnlyList<Case> GetAll() => casesRepository.GetAll();

        public Case? GetById(Guid id) => casesRepository.GetById(id);

        public Case? Update(Guid id, UpdateCaseRequest request)
        {
            var caseItem = GetById(id);
            if (caseItem == null) return null;

            caseItem.UpdateDetails(request.Title, request.Area, request.Description, request.Notes);
            casesRepository.Save();
            return caseItem;
        }

        public Case? ChangeStatus(Guid id, ChangeStatusRequest request)
        {
            var caseItem = GetById(id);
            if (caseItem == null) return null;

            caseItem.ChangeStatus(request.Status);
            casesRepository.Save();
            return caseItem;
        }

        public Case? AddLawyer(Guid id, AddLawyerRequest request)
        {
            var caseItem = GetById(id);
            if (caseItem == null) return null;

            var lawyer = usersRepository.GetById(request.LawyerId);
            if (lawyer is not Lawyer)
                throw new ArgumentException("El abogado indicado no es válido.");

            caseItem.AddLawyer((Lawyer)lawyer);
            casesRepository.Save();
            return caseItem;
        }

        public Case? RemoveLawyer(Guid id, RemoveLawyerRequest request)
        {
            var caseItem = GetById(id);
            if (caseItem == null) return null;

            if (usersRepository.GetById(request.LawyerId) is not Lawyer)
                throw new ArgumentException("El abogado indicado no es válido.");

            caseItem.RemoveLawyer(request.LawyerId);
            casesRepository.Save();
            return caseItem;
        }

        public bool Delete(Guid id)
        {
            var caseItem = GetById(id);
            if (caseItem == null) return false;

            var hasActiveAppointments = appointmentsRepository.GetAll()
                .Any(a => a.CaseId == id
                    && a.EffectiveStatus != AppointmentStatus.Cancelado && a.EffectiveStatus != AppointmentStatus.Finalizado);

            if (hasActiveAppointments)
                throw new InvalidOperationException($"El expediente con id {id} tiene turnos activos asociados.");

            caseItem.Deactivate();
            casesRepository.Save();
            return true;
        }
    }
}