using System.ComponentModel.DataAnnotations;
using LegalManager.Domain;

namespace LegalManager.Application.DTOs
{
    public record UpdateCaseRequest(
        [param: StringLength(FieldLengths.Title)] string Title,
        [param: StringLength(FieldLengths.Area)] string Area,
        [param: StringLength(FieldLengths.LongText)] string? Description,
        [param: StringLength(FieldLengths.LongText)] string? Notes);
}