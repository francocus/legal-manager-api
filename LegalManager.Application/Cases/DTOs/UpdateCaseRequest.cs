using System.ComponentModel.DataAnnotations;
using LegalManager.Domain.ValueObjects;
using LegalManager.Application.Common.Validation;

namespace LegalManager.Application.Cases.DTOs
{
    public record UpdateCaseRequest(
        [param: StringLength(FieldLengths.Title)] string Title,
        [param: StringLength(FieldLengths.Area), LegalArea] string Area,
        [param: StringLength(FieldLengths.LongText)] string? Description,
        [param: StringLength(FieldLengths.LongText)] string? Notes);
}