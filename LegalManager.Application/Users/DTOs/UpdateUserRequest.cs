using System.ComponentModel.DataAnnotations;
using LegalManager.Domain.ValueObjects;

namespace LegalManager.Application.Users.DTOs
{
    public record UpdateUserRequest(
        [param: StringLength(FieldLengths.PersonName)] string FirstName,
        [param: StringLength(FieldLengths.PersonName)] string LastName,
        [param: StringLength(FieldLengths.Dni)] string Dni,
        [param: StringLength(FieldLengths.Email)] string Email);
}