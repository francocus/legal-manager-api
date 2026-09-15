namespace LegalManager.Domain.Entities
{
    public enum DocumentType
    {
        Escrito,
        Expediente,
        Contrato,
        Prueba,
        Generado
    }

    public class Document
    {
        public Guid Id { get; private set; }
        public Guid CaseId { get; private set; }
        public string FileName { get; private set; }
        public string FilePath { get; private set; }
        public string ContentType { get; private set; }
        public long SizeBytes { get; private set; }
        public DocumentType Type { get; private set; }
        public Guid UploadedByUserId { get; private set; }
        public DateOnly UploadDate { get; private set; }
        public bool Active { get; private set; }

        private Document()
        {
            FileName = string.Empty;
            FilePath = string.Empty;
            ContentType = string.Empty;
        }

        public Document(Guid caseId, string fileName, string filePath, string contentType, long sizeBytes, DocumentType type, Guid uploadedByUserId)
        {
            if (caseId == Guid.Empty)
                throw new ArgumentException("El documento necesita un expediente asociado.", nameof(caseId));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("El nombre del archivo es obligatorio.", nameof(fileName));

            if (!contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Solo se permiten archivos PDF.", nameof(contentType));

            if (sizeBytes <= 0)
                throw new ArgumentException("El archivo está vacío.", nameof(sizeBytes));

            if (sizeBytes > 10 * 1024 * 1024)
                throw new ArgumentException("El archivo supera el tamaño máximo permitido (10 MB).", nameof(sizeBytes));

            if (uploadedByUserId == Guid.Empty)
                throw new ArgumentException("El documento necesita un usuario que lo suba.", nameof(uploadedByUserId));

            Id = Guid.NewGuid();
            CaseId = caseId;
            FileName = fileName;
            FilePath = filePath;
            ContentType = contentType;
            SizeBytes = sizeBytes;
            Type = type;
            UploadedByUserId = uploadedByUserId;
            UploadDate = DateOnly.FromDateTime(DateTime.Now);
            Active = true;
        }

        public void Deactivate()
        {
            if (!Active)
                throw new InvalidOperationException("El documento ya está inactivo.");

            Active = false;
        }
    }
}
