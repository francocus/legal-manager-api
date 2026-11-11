namespace LegalManager.Application.Auth
{
    public class EmailOptions
    {
        public const string SectionName = "Email";

        public string? FrontBaseUrl { get; set; }

        public string ConfirmationPath { get; set; } = "/confirmar-email";

        public string ResetPath { get; set; } = "/restablecer-contrasena";

        public int ConfirmationTokenLifetimeHours { get; set; } = 24;

        public int ResetTokenLifetimeMinutes { get; set; } = 60;
    }
}