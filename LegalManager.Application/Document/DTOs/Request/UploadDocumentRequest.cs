using LegalManager.Domain.Entities;

namespace LegalManager.Application.DTOs
{
    public class UploadDocumentRequest
    {
        public Guid CaseId { get; set; }
        public DocumentType Type { get; set; }
        public Guid UploadedByUserId { get; set; }
        public Stream FileContent { get; set; } = null!;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Length { get; set; }
    }
}
