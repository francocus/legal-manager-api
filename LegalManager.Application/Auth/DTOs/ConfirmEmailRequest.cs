using System.ComponentModel.DataAnnotations;

namespace LegalManager.Application.Auth.DTOs
{
    public record ConfirmEmailRequest(
        [param: StringLength(256)] string Token);
}
