namespace LegalManager.Application.Auth
{
    // Options for the account emails (confirmation + password reset links).
    // FrontBaseUrl is the frontend origin the emailed links point at; when it is
    // missing the flow keeps working and the emailed URL is relative-only.
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
