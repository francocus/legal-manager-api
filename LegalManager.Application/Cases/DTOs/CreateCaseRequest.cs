using System.ComponentModel.DataAnnotations;
using LegalManager.Domain;
using LegalManager.Domain.Enums;
using LegalManager.Domain.ValueObjects;
using LegalManager.Domain.Exceptions;
using LegalManager.Application.Common.Validation;

namespace LegalManager.Application.Cases.DTOs
{
    public record CreateCaseRequest(
        [param: StringLength(FieldLengths.CaseNumber)] string CaseNumber,
        [param: StringLength(FieldLengths.Title)] string Title,
        [property: StringLength(FieldLengths.Area), LegalArea] string Area,
        DateOnly StartDate,
        [param: StringLength(FieldLengths.LongText)] string? Description,
        [param: StringLength(FieldLengths.LongText)] string? Notes,
        Guid ClientId,
        Guid LawyerId);
}