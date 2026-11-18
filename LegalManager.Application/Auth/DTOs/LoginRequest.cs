using System.ComponentModel.DataAnnotations;
using LegalManager.Domain.ValueObjects;

namespace LegalManager.Application.Auth.DTOs
{
    public record LoginRequest(
        [param: StringLength(FieldLengths.Email)] string Email,
        [param: StringLength(FieldLengths.Password)] string Password);
}