using System.ComponentModel.DataAnnotations;
using LegalManager.Domain;
using LegalManager.Domain.Enums;
using LegalManager.Domain.ValueObjects;
using LegalManager.Domain.Exceptions;
using LegalManager.Application.Common.Validation;

namespace LegalManager.Application.Cases.DTOs
{
    public record UpdateCaseRequest(
        [param: StringLength(FieldLengths.Title)] string Title,
        [property: StringLength(FieldLengths.Area), LegalArea] string Area,
        [param: StringLength(FieldLengths.LongText)] string? Description,
        [param: StringLength(FieldLengths.LongText)] string? Notes);
}