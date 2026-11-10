using System.ComponentModel.DataAnnotations;

namespace LegalManager.Application.DTOs
{
    public record ConfirmEmailRequest(
        [param: StringLength(256)] string Token);
}
