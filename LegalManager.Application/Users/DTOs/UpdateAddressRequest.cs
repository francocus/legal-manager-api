using System.ComponentModel.DataAnnotations;
using LegalManager.Domain.ValueObjects;

namespace LegalManager.Application.Users.DTOs
{
    public record UpdateAddressRequest([param: StringLength(FieldLengths.Address)] string? Address);
}