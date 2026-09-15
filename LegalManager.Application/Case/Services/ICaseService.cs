using LegalManager.Application.DTOs;

namespace LegalManager.Application.Interfaces
{
    public interface ICaseService
    {
        CaseResponse Create(CreateCaseRequest request);

        IReadOnlyList<CaseResponse> GetAll();

        CaseResponse? GetById(Guid id);

        CaseResponse? Update(Guid id, UpdateCaseRequest request);

        CaseResponse? ChangeStatus(Guid id, ChangeStatusRequest request);

        CaseResponse? AddLawyer(Guid id, AddLawyerRequest request);

        CaseResponse? RemoveLawyer(Guid id, RemoveLawyerRequest request);

        bool Delete(Guid id);
    }
}