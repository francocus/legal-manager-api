using System.ComponentModel.DataAnnotations;
using LegalManager.Domain.ValueObjects;
using LegalManager.Application.Common.Validation;

namespace LegalManager.Application.Cases.DTOs
{
    public record CreateCaseRequest(
        [param: StringLength(FieldLengths.CaseNumber)] string CaseNumber,
        [param: StringLength(FieldLengths.Title)] string Title,
        [param: StringLength(FieldLengths.Area), LegalArea] string Area,
        DateOnly StartDate,
        [param: StringLength(FieldLengths.LongText)] string? Description,
        [param: StringLength(FieldLengths.LongText)] string? Notes,
        Guid ClientId,
        Guid LawyerId);
}