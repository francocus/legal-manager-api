using System.ComponentModel.DataAnnotations;
using LegalManager.Domain.Constants;

namespace LegalManager.Application.Common.Validation
{
    /// <summary>
    /// Accepts empty values only when explicitly allowed, and otherwise requires
    /// every comma-separated token to be one of the five canonical legal areas.
    /// </summary>
    public sealed class LegalAreaAttribute : ValidationAttribute
    {
        public bool AllowEmpty { get; set; }

        public LegalAreaAttribute()
        {
            ErrorMessage = $"El área debe ser una de: {string.Join(", ", LegalAreas.All)}.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var raw = value as string;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return AllowEmpty ? ValidationResult.Success : new ValidationResult(ErrorMessage);
            }

            var parts = raw.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Any(part => string.IsNullOrWhiteSpace(part) || !LegalAreas.Contains(part)))
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}
