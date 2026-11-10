using System.Security.Cryptography;
using System.Text;
using LegalManager.Application.Auth;
using LegalManager.Application.Interfaces;
using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace LegalManager.Application.Services
{
    // Token semantics, uniform for confirm + reset: 32 random bytes encoded as base64url
    // without padding (~43 chars) travel RAW in the email link; storage keeps only the hex
    // SHA-256 hash (64 chars) plus the UTC expiry, and the hash is cleared on first use.
    public class AccountConfirmationService(
        IUserRepository usersRepository,
        IAccountEmailService emailSender,
        IOptions<EmailOptions> emailOptions,
        IPasswordHasher passwordHasher)
    {
        // Sets the token hash + expiry on the entity but does not save: the caller saves
        // and then emails the returned raw token inside the link.
        public Task<string> StartForNewUserAsync(User user, CancellationToken cancellationToken = default)
        {
            var rawToken = GenerateRawToken();
            user.StartEmailConfirmation(HashToken(rawToken), DateTime.UtcNow.AddHours(emailOptions.Value.ConfirmationTokenLifetimeHours));

            return Task.FromResult(rawToken);
        }

        // FrontBaseUrl trimmed of its trailing slash + the feature path + the raw token.
        // With no FrontBaseUrl the base part is empty and the URL stays relative; the SMTP
        // sender logs it, so a clone without frontend/base config can still be driven by hand.
        public static string BuildUrl(EmailOptions options, string path, string rawToken)
        {
            var baseUrl = string.IsNullOrWhiteSpace(options.FrontBaseUrl) ? string.Empty : options.FrontBaseUrl.TrimEnd('/');

            return $"{baseUrl}{path}?token={rawToken}";
        }

        public async Task NotifyConfirmationAsync(User user, string rawToken, CancellationToken cancellationToken = default)
        {
            var url = BuildUrl(emailOptions.Value, emailOptions.Value.ConfirmationPath, rawToken);

            await emailSender.SendEmailConfirmationAsync(user.Email, url, cancellationToken);
        }

        public async Task<User> ConfirmByTokenAsync(string rawToken, CancellationToken cancellationToken = default)
        {
            var tokenHash = HashToken(rawToken ?? string.Empty);
            var user = await usersRepository.GetByEmailConfirmationTokenAsync(tokenHash, cancellationToken);

            if (user == null
                || user.EmailConfirmationToken is null
                || user.EmailConfirmationTokenExpiresAt is null
                || user.EmailConfirmationTokenExpiresAt <= DateTime.UtcNow)
                throw new AccountLinkException("El enlace es inválido o expiró. Pedí uno nuevo.");

            user.ConfirmEmail();
            await usersRepository.SaveAsync(cancellationToken);

            return user;
        }

        public async Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("El email es obligatorio.", nameof(email));

            var user = await usersRepository.GetByEmailAsync(email, cancellationToken);

            // Missing or inactive account: return silently to avoid leaking registered emails.
            if (user == null || !user.Active)
                return;

            var rawToken = GenerateRawToken();
            user.StartPasswordReset(HashToken(rawToken), DateTime.UtcNow.AddMinutes(emailOptions.Value.ResetTokenLifetimeMinutes));
            await usersRepository.SaveAsync(cancellationToken);

            var url = BuildUrl(emailOptions.Value, emailOptions.Value.ResetPath, rawToken);
            await emailSender.SendPasswordResetAsync(user.Email, url, cancellationToken);
        }

        public async Task<User> ResetPasswordAsync(string rawToken, string newPassword, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentException("La contraseña es obligatoria.", nameof(newPassword));

            var tokenHash = HashToken(rawToken ?? string.Empty);
            var user = await usersRepository.GetByPasswordResetTokenAsync(tokenHash, cancellationToken);

            if (user == null || !user.VerifyPasswordResetToken(tokenHash, DateTime.UtcNow))
                throw new AccountLinkException("El enlace es inválido o expiró. Pedí uno nuevo.");

            user.CompletePasswordReset(passwordHasher.Hash(newPassword));
            await usersRepository.SaveAsync(cancellationToken);

            return user;
        }

        public async Task ResendConfirmationAsync(string email, CancellationToken cancellationToken = default)
        {
            var user = await usersRepository.GetByEmailAsync(email, cancellationToken);

            // Only pending active accounts get a new link; everything else stays silent.
            if (user == null || !user.Active || user.EmailConfirmed)
                return;

            var rawToken = await StartForNewUserAsync(user, cancellationToken);
            await usersRepository.SaveAsync(cancellationToken);

            await NotifyConfirmationAsync(user, rawToken, cancellationToken);
        }

        private static string GenerateRawToken()
        {
            // 32 random bytes -> base64url without padding (~43 chars).
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        private static string HashToken(string rawToken)
        {
            // Hex SHA-256 of the raw token (64 chars). Never persisted in raw form.
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        }
    }
}
