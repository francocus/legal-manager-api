using System.ComponentModel.DataAnnotations;
using LegalManager.Domain;

namespace LegalManager.Application.DTOs
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
