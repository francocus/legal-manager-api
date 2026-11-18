using System.ComponentModel.DataAnnotations;
using LegalManager.Domain.ValueObjects;

namespace LegalManager.Application.Auth.DTOs
{
    public record ForgotPasswordRequest(
        [param: StringLength(FieldLengths.Email)] string Email);
}
