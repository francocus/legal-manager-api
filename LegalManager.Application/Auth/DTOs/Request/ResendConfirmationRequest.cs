using System.ComponentModel.DataAnnotations;
using LegalManager.Domain;

namespace LegalManager.Application.DTOs
{
    public record ResendConfirmationRequest(
        [param: StringLength(FieldLengths.Email)] string Email);
}
