using System.ComponentModel.DataAnnotations;
using LegalManager.Domain;
using LegalManager.Domain.Enums;
using LegalManager.Domain.ValueObjects;
using LegalManager.Domain.Exceptions;

namespace LegalManager.Application.Auth.DTOs
{
    public record ResendConfirmationRequest(
        [param: StringLength(FieldLengths.Email)] string Email);
}
