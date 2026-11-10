using System.ComponentModel.DataAnnotations;
using LegalManager.Domain;

namespace LegalManager.Application.DTOs
{
    public record ForgotPasswordRequest(
        [param: StringLength(FieldLengths.Email)] string Email);
}
