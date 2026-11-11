using System.ComponentModel.DataAnnotations;
using LegalManager.Domain;
using LegalManager.Domain.Enums;
using LegalManager.Domain.ValueObjects;
using LegalManager.Domain.Exceptions;

namespace LegalManager.Application.Auth.DTOs
{
    // Public registration payload. It maps to the same fields as CreateClientRequest
    // (phone/address excluded: they are optional user-managed data).
    public record RegisterRequest(
        [param: StringLength(FieldLengths.PersonName)] string FirstName,
        [param: StringLength(FieldLengths.PersonName)] string LastName,
        [param: StringLength(FieldLengths.Dni)] string Dni,
        [param: StringLength(FieldLengths.Email)] string Email,
        [param: StringLength(FieldLengths.Password)] string Password);
}
