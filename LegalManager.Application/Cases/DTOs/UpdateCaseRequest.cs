using System.ComponentModel.DataAnnotations;
using LegalManager.Domain;
using LegalManager.Domain.Enums;
using LegalManager.Domain.ValueObjects;
using LegalManager.Domain.Exceptions;

namespace LegalManager.Application.Cases.DTOs
{
    public record UpdateCaseRequest(
        [param: StringLength(FieldLengths.Title)] string Title,
        [param: StringLength(FieldLengths.Area)] string Area,
        [param: StringLength(FieldLengths.LongText)] string? Description,
        [param: StringLength(FieldLengths.LongText)] string? Notes);
}