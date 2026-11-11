using System.ComponentModel.DataAnnotations;
using LegalManager.Domain;
using LegalManager.Domain.Enums;
using LegalManager.Domain.ValueObjects;
using LegalManager.Domain.Exceptions;

namespace LegalManager.Application.Users.DTOs
{
    public record UpdateBarNumberRequest([param: StringLength(FieldLengths.BarNumber)] string BarNumber);
}