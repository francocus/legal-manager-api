using System.ComponentModel.DataAnnotations;
using LegalManager.Domain;
using LegalManager.Domain.Enums;
using LegalManager.Domain.ValueObjects;
using LegalManager.Domain.Exceptions;

namespace LegalManager.Application.Auth.DTOs
{
    public record ResetPasswordRequest(
        [param: StringLength(256)] string Token,
        [param: StringLength(FieldLengths.Password)] string NewPassword);
}
