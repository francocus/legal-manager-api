using System.Text;

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
        public bool GeneratedByAI { get; private set; }
        public Guid? ReviewedByUserId { get; private set; }
        public DateOnly? ReviewedAt { get; private set; }

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
            GeneratedByAI = false;
        }

        public static Document CreateAiSummary(Guid caseId, string fileName, string filePath, string content, Guid generatedByUserId)
        {
            if (caseId == Guid.Empty)
                throw new ArgumentException("El documento necesita un expediente asociado.", nameof(caseId));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("El nombre del archivo es obligatorio.", nameof(fileName));

            if (generatedByUserId == Guid.Empty)
                throw new ArgumentException("El documento necesita un usuario que lo genere.", nameof(generatedByUserId));

            return new Document
            {
                Id = Guid.NewGuid(),
                CaseId = caseId,
                FileName = fileName,
                FilePath = filePath,
                ContentType = "application/pdf",
                SizeBytes = Encoding.UTF8.GetByteCount(content),
                Type = DocumentType.Generado,
                UploadedByUserId = generatedByUserId,
                UploadDate = DateOnly.FromDateTime(DateTime.Now),
                Active = true,
                GeneratedByAI = true
            };
        }

        public void Review(Guid reviewedByUserId)
        {
            if (reviewedByUserId == Guid.Empty)
                throw new ArgumentException("El usuario que revisa es obligatorio.", nameof(reviewedByUserId));

            if (!GeneratedByAI)
                throw new InvalidOperationException("Solo los documentos generados por IA requieren revisión.");

            if (ReviewedAt != null)
                throw new InvalidOperationException("El documento ya fue revisado.");

            ReviewedByUserId = reviewedByUserId;
            ReviewedAt = DateOnly.FromDateTime(DateTime.Now);
        }

        public void Deactivate()
        {
            if (!Active)
                throw new InvalidOperationException("El documento ya está inactivo.");

            Active = false;
        }
    }
}
