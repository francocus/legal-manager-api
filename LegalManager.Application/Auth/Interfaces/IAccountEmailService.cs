namespace LegalManager.Application.Auth.Interfaces
{
    // Port for account emails, implemented by Infrastructure (SMTP) and consumed by
    // the confirmation service. Receives the ready-to-use URL, never the raw token.
    public interface IAccountEmailService
    {
        Task SendEmailConfirmationAsync(string to, string confirmationUrl, CancellationToken cancellationToken = default);

        Task SendPasswordResetAsync(string to, string resetUrl, CancellationToken cancellationToken = default);
    }
}
