namespace LegalManager.Application.Auth.Interfaces
{
    public interface IAccountEmailService
    {
        Task SendEmailConfirmationAsync(string to, string confirmationUrl, CancellationToken cancellationToken = default);

        Task SendPasswordResetAsync(string to, string resetUrl, CancellationToken cancellationToken = default);
    }
}