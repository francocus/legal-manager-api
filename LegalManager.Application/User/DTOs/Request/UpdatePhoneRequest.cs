using System.ComponentModel.DataAnnotations;
using LegalManager.Domain;

namespace LegalManager.Application.DTOs
{
    public record UpdatePhoneRequest([param: StringLength(FieldLengths.Phone)] string? Phone);
}