using System.ComponentModel.DataAnnotations;
using LegalManager.Domain;

namespace LegalManager.Application.DTOs
{
    public record UpdateBarNumberRequest([param: StringLength(FieldLengths.BarNumber)] string BarNumber);
}