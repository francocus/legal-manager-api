using System.ComponentModel.DataAnnotations;
using LegalManager.Domain;
using LegalManager.Domain.Enums;
using LegalManager.Domain.ValueObjects;
using LegalManager.Domain.Exceptions;

namespace LegalManager.Application.Users.DTOs
{
    public record CreateLawyerRequest(
        [param: StringLength(FieldLengths.PersonName)] string FirstName,
        [param: StringLength(FieldLengths.PersonName)] string LastName,
        [param: StringLength(FieldLengths.Dni)] string Dni,
        [param: StringLength(FieldLengths.Email)] string Email,
        [param: StringLength(FieldLengths.Password)] string Password,
        [param: StringLength(FieldLengths.BarNumber)] string BarNumber,
        [param: StringLength(FieldLengths.Phone)] string? Phone,
        List<string>? Specialties);
}