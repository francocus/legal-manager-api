using LegalManager.Domain.Entities;
using LegalManager.Domain.Enums;

namespace LegalManager.Application.Cases.DTOs
{
    public record ChangeStatusRequest(CaseStatus Status);
}