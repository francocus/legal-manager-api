namespace LegalManager.Domain
{
    /// <summary>
    /// Maximum lengths of text fields, in characters.
    /// Single source of truth: used by the <c>[StringLength]</c> annotations on the DTOs
    /// (Application) and the <c>HasMaxLength</c> EF mapping (Infrastructure), so the two
    /// layers cannot diverge. Without this every text column stays <c>nvarchar(max)</c>.
    /// </summary>
    public static class FieldLengths
    {
        public const int PersonName = 100;
        public const int Dni = 20;
        public const int Email = 256;
        public const int Password = 100;
        public const int PasswordHash = 100;
        public const int Phone = 30;
        public const int Address = 200;
        public const int BarNumber = 50;
        public const int CaseNumber = 50;
        public const int Title = 200;
        public const int Area = 100;
        public const int LongText = 4000;
        public const int Reason = 500;
        public const int Location = 200;
        public const int FileName = 255;
        public const int FilePath = 1000;
        public const int ContentType = 100;
    }
}