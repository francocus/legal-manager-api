using System.ComponentModel.DataAnnotations;
using LegalManager.Domain.ValueObjects;

namespace LegalManager.Application.Users.DTOs
{
    public record UpdatePhoneRequest([param: StringLength(FieldLengths.Phone)] string? Phone);
}