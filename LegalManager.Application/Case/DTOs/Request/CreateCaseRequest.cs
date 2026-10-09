using System.ComponentModel.DataAnnotations;
using LegalManager.Domain;

namespace LegalManager.Application.DTOs
{
    public record CreateCaseRequest(
        [param: StringLength(FieldLengths.CaseNumber)] string CaseNumber,
        [param: StringLength(FieldLengths.Title)] string Title,
        [param: StringLength(FieldLengths.Area)] string Area,
        DateOnly StartDate,
        [param: StringLength(FieldLengths.LongText)] string? Description,
        [param: StringLength(FieldLengths.LongText)] string? Notes,
        Guid ClientId,
        Guid LawyerId);
}