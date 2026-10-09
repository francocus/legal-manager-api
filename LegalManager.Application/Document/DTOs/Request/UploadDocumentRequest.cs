using System.ComponentModel.DataAnnotations;
using LegalManager.Domain;
using LegalManager.Domain.Entities;

namespace LegalManager.Application.DTOs
{
    public class UploadDocumentRequest
    {
        public Guid CaseId { get; set; }

        public DocumentType Type { get; set; }

        public Stream FileContent { get; set; } = null!;

        [StringLength(FieldLengths.FileName)]
        public string FileName { get; set; } = string.Empty;

        [StringLength(FieldLengths.ContentType)]
        public string ContentType { get; set; } = string.Empty;

        public long Length { get; set; }
    }
}