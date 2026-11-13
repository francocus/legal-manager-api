using System.ComponentModel.DataAnnotations;
using LegalManager.Domain.Interfaces;
using LegalManager.Domain.ValueObjects;

namespace LegalManager.Domain.Entities
{
    public abstract class User : IEntity
    {
        public Guid Id { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string FullName => $"{FirstName} {LastName}";
        public string Dni { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public DateOnly RegistrationDate { get; private set; }
        public bool Active { get; private set; }
        public DateTime? DeactivatedAt { get; private set; }
        public bool EmailConfirmed { get; private set; }

        [MaxLength(FieldLengths.EmailTokenHash)]
        public string? EmailConfirmationToken { get; private set; }

        public DateTime? EmailConfirmationTokenExpiresAt { get; private set; }

        [MaxLength(FieldLengths.EmailTokenHash)]
        public string? PasswordResetToken { get; private set; }

        public DateTime? PasswordResetTokenExpiresAt { get; private set; }

        protected User()
        {
            FirstName = string.Empty;
            LastName = string.Empty;
            Dni = string.Empty;
            Email = string.Empty;
            PasswordHash = string.Empty;
        }

        protected User(string firstName, string lastName, string dni, string email, string passwordHash)
            : this()
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("El nombre es obligatorio.", nameof(firstName));

            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("El apellido es obligatorio.", nameof(lastName));

            if (string.IsNullOrWhiteSpace(dni))
                throw new ArgumentException("El DNI es obligatorio.", nameof(dni));

            dni = ValidateDniFormat(dni);

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("El email es obligatorio.", nameof(email));

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("El hash de la contraseña es obligatorio.", nameof(passwordHash));

            Id = Guid.NewGuid();
            FirstName = firstName;
            LastName = lastName;
            Dni = dni;
            Email = email;
            PasswordHash = passwordHash;
            RegistrationDate = DateOnly.FromDateTime(DateTime.Now);
            Active = true;
        }

        public void UpdateDetails(string firstName, string lastName, string dni, string email)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("El nombre es obligatorio.", nameof(firstName));

            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("El apellido es obligatorio.", nameof(lastName));

            if (string.IsNullOrWhiteSpace(dni))
                throw new ArgumentException("El DNI es obligatorio.", nameof(dni));

            dni = ValidateDniFormat(dni);

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("El email es obligatorio.", nameof(email));

            FirstName = firstName;
            LastName = lastName;
            Dni = dni;
            Email = email;
        }

        public void Deactivate()
        {
            if (!Active)
                throw new InvalidOperationException("El usuario ya está inactivo.");

            Active = false;
            DeactivatedAt = DateTime.UtcNow;
        }

        public void Reactivate()
        {
            if (Active)
                throw new InvalidOperationException("El usuario ya está activo.");

            Active = true;
            DeactivatedAt = null;
        }

        public void StartEmailConfirmation(string tokenHash, DateTime expiresAtUtc)
        {
            if (string.IsNullOrWhiteSpace(tokenHash))
                throw new ArgumentException("El hash del token de confirmación es obligatorio.", nameof(tokenHash));

            if (expiresAtUtc <= DateTime.UtcNow)
                throw new ArgumentException("La fecha de vencimiento debe ser futura.", nameof(expiresAtUtc));

            EmailConfirmationToken = tokenHash;
            EmailConfirmationTokenExpiresAt = expiresAtUtc;
        }

        public void ConfirmEmail()
        {
            if (EmailConfirmed)
                throw new InvalidOperationException("La cuenta ya está confirmada.");

            EmailConfirmed = true;
            EmailConfirmationToken = null;
            EmailConfirmationTokenExpiresAt = null;
        }

        public void StartPasswordReset(string tokenHash, DateTime expiresAtUtc)
        {
            if (string.IsNullOrWhiteSpace(tokenHash))
                throw new ArgumentException("El hash del token de restablecimiento es obligatorio.", nameof(tokenHash));

            if (expiresAtUtc <= DateTime.UtcNow)
                throw new ArgumentException("La fecha de vencimiento debe ser futura.", nameof(expiresAtUtc));

            PasswordResetToken = tokenHash;
            PasswordResetTokenExpiresAt = expiresAtUtc;
        }

        public bool VerifyPasswordResetToken(string tokenHash, DateTime nowUtc)
        {
            if (!Active)
                return false;

            return PasswordResetToken is not null
                && string.Equals(PasswordResetToken, tokenHash, StringComparison.Ordinal)
                && PasswordResetTokenExpiresAt is not null
                && PasswordResetTokenExpiresAt > nowUtc;
        }

        public void CompletePasswordReset(string newPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
                throw new ArgumentException("El hash de la contraseña es obligatorio.", nameof(newPasswordHash));

            PasswordHash = newPasswordHash;
            PasswordResetToken = null;
            PasswordResetTokenExpiresAt = null;
            EmailConfirmed = true;
        }

        protected void CopyRoleChangeFrom(User other)
        {
            Id = other.Id;
            FirstName = other.FirstName;
            LastName = other.LastName;
            Dni = other.Dni;
            Email = other.Email;
            PasswordHash = other.PasswordHash;
            RegistrationDate = other.RegistrationDate;
            Active = other.Active;
            DeactivatedAt = other.DeactivatedAt;
            EmailConfirmed = other.EmailConfirmed;
            EmailConfirmationToken = other.EmailConfirmationToken;
            EmailConfirmationTokenExpiresAt = other.EmailConfirmationTokenExpiresAt;
            PasswordResetToken = other.PasswordResetToken;
            PasswordResetTokenExpiresAt = other.PasswordResetTokenExpiresAt;
        }

        private static string ValidateDniFormat(string dni)
        {
            var normalized = DniFormat.Normalize(dni);
            if (normalized.Length is < 7 or > 8 || normalized.Any(c => !char.IsDigit(c)))
                throw new ArgumentException("Formato de DNI inválido. Se esperan 7 u 8 dígitos, opcionalmente separados por puntos o espacios.", nameof(dni));

            return normalized;
        }
    }
}