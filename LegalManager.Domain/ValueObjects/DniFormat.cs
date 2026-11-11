namespace LegalManager.Domain.ValueObjects
{
    public static class DniFormat
    {
        public static string Normalize(string dni)
            => (dni ?? string.Empty).Replace(".", "").Replace(" ", "");
    }
}