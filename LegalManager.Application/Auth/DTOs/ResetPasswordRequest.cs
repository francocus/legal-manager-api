using System.ComponentModel.DataAnnotations;
using LegalManager.Domain.ValueObjects;

namespace LegalManager.Application.Auth.DTOs
{
    public record ResetPasswordRequest(
        [param: StringLength(256)] string Token,
        [param: StringLength(FieldLengths.Password)] string NewPassword);
}
