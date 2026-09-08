using LegalManager.Application.DTOs;
using LegalManager.Domain.Entities;

namespace LegalManager.Application.Interfaces
{
    public interface ICaseService
    {
        Case Create(CreateCaseRequest request);

        IReadOnlyList<Case> GetAll();

        Case? GetById(Guid id);

        Case? Update(Guid id, UpdateCaseRequest request);

        Case? ChangeStatus(Guid id, ChangeStatusRequest request);

        Case? AddLawyer(Guid id, AddLawyerRequest request);

        Case? RemoveLawyer(Guid id, RemoveLawyerRequest request);

        bool Delete(Guid id);
    }
}