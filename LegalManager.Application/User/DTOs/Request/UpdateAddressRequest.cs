using System.ComponentModel.DataAnnotations;
using LegalManager.Domain;

namespace LegalManager.Application.DTOs
{
    public record UpdateAddressRequest([param: StringLength(FieldLengths.Address)] string? Address);
}