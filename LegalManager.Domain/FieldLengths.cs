namespace LegalManager.Domain
{
    /// <summary>
    /// Límites de longitud de los campos de texto, en caracteres.
    /// Fuente única: la usan las anotaciones <c>[StringLength]</c> de los DTOs (Application)
    /// y el mapeo <c>HasMaxLength</c> de EF (Infrastructure), para que las dos capas no
    /// puedan divergir. Sin esto todo texto queda en <c>nvarchar(max)</c>.
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