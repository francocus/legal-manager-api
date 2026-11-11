using LegalManager.Application.Auth;
using LegalManager.Application.Auth.Interfaces;
using LegalManager.Application.Users.Interfaces;
using LegalManager.Application.Cases.Interfaces;
using LegalManager.Application.Appointments.Interfaces;
using LegalManager.Application.Documents.Interfaces;
using LegalManager.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LegalManager.Infrastructure.Email
{
    public class SmtpOptions
    {
        public const string SectionName = "Smtp";

        public string? Host { get; set; }

        public int Port { get; set; } = 587;

        public string? User { get; set; }

        public string? Password { get; set; }

        public string? From { get; set; }

        public bool UseSsl { get; set; } = true;
    }

    public class SmtpAccountEmailService(
        IOptions<SmtpOptions> smtpOptions,
        IOptions<EmailOptions> emailOptions,
        ILogger<SmtpAccountEmailService> logger) : IAccountEmailService
    {
        public async Task SendEmailConfirmationAsync(string to, string confirmationUrl, CancellationToken cancellationToken = default)
        {
            var hours = emailOptions.Value.ConfirmationTokenLifetimeHours;

            await SendAsync(
                to,
                subject: "Confirmá tu email de Legal Manager",
                heading: "Confirmá tu email",
                paragraph: "Se creó una cuenta con este email en Legal Manager. Confirmalo para poder iniciar sesión.",
                ctaLabel: "Confirmar email",
                url: confirmationUrl,
                validity: $"El enlace vence en {hours} horas.",
                cancellationToken);
        }

        public async Task SendPasswordResetAsync(string to, string resetUrl, CancellationToken cancellationToken = default)
        {
            var minutes = emailOptions.Value.ResetTokenLifetimeMinutes;

            await SendAsync(
                to,
                subject: "Restablecé tu contraseña de Legal Manager",
                heading: "Restablecé tu contraseña",
                paragraph: "Se pidió un restablecimiento de contraseña para esta cuenta. Si fuiste vos, podés setear una nueva con el siguiente enlace.",
                ctaLabel: "Restablecer contraseña",
                url: resetUrl,
                validity: $"El enlace vence en {minutes} minutos.",
                cancellationToken);
        }

        private async Task SendAsync(string to, string subject, string heading, string paragraph, string ctaLabel, string url, string validity, CancellationToken cancellationToken)
        {
            var options = smtpOptions.Value;

            if (string.IsNullOrWhiteSpace(options.Host))
            {
                logger.LogWarning("[DEV] Smtp no configurado. To={To}, Subject={Subject}, Body={Body}",
                    to, subject, BuildPlainText(heading, paragraph, url, validity));
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Legal Manager", options.From));
            message.To.Add(new MailboxAddress(to, to));
            message.Subject = subject;
            message.Body = new MultipartAlternative
            {
                new TextPart("plain") { Text = BuildPlainText(heading, paragraph, url, validity) },
                new TextPart("html") { Text = BuildHtml(heading, paragraph, ctaLabel, url, validity) }
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(options.Host, options.Port, options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, cancellationToken);

            if (!string.IsNullOrWhiteSpace(options.User))
                await client.AuthenticateAsync(options.User, options.Password, cancellationToken);

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }

        private static string BuildPlainText(string heading, string paragraph, string url, string validity)
            => $"""
               {heading}

               {paragraph}

               {url}

               {validity}

               Si no fuiste vos, ignorá este mensaje y tu cuenta queda como está.
               """;

        private static string BuildHtml(string heading, string paragraph, string ctaLabel, string url, string validity)
            => $"""
               <html>
                 <body style="margin:0;padding:24px;background:#f6f7f9;font-family:Arial,Helvetica,sans-serif;color:#1f2933;">
                   <div style="max-width:480px;margin:0 auto;background:#ffffff;border-radius:12px;padding:32px;">
                     <h1 style="font-size:22px;margin:0 0 16px;">{heading}</h1>
                     <p style="font-size:14px;line-height:1.6;margin:0 0 24px;">{paragraph}</p>
                     <p style="margin:0 0 24px;">
                       <a href="{url}" style="background:#2f6feb;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none;">{ctaLabel}</a>
                     </p>
                     <p style="font-size:13px;color:#52606d;margin:0 0 8px;">{validity}</p>
                     <p style="font-size:12px;color:#9aa5b1;margin:0;">Si no fuiste vos, ignorá este mensaje y tu cuenta queda como está.</p>
                   </div>
                 </body>
               </html>
               """;
    }
}