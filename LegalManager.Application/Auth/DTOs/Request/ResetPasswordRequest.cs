using System.ComponentModel.DataAnnotations;
using LegalManager.Domain;

namespace LegalManager.Application.DTOs
{
    public record ResetPasswordRequest(
        [param: StringLength(256)] string Token,
        [param: StringLength(FieldLengths.Password)] string NewPassword);
}
