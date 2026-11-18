using System.ComponentModel.DataAnnotations;
using LegalManager.Domain.ValueObjects;

namespace LegalManager.Application.Users.DTOs
{
    public record UpdateBarNumberRequest([param: StringLength(FieldLengths.BarNumber)] string BarNumber);
}