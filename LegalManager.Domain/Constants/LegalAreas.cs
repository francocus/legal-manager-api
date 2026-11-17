namespace LegalManager.Domain.Constants
{
    /// <summary>
    /// Single source of truth for practice areas. The UI selector, the API
    /// validation and any seeding must use this same vocabulary.
    /// </summary>
    public static class LegalAreas
    {
        public static readonly string[] All = ["Civil", "Comercial", "Penal", "Laboral", "Familia"];

        public static bool Contains(string value) =>
            All.Contains(value, StringComparer.Ordinal);
    }
}
