using LegalManager.Domain.Entities;

namespace LegalManager.Application.DTOs
{
    public record DocumentResponse(
        Guid Id,
        Guid CaseId,
        string FileName,
        string ContentType,
        long SizeBytes,
        DocumentType Type,
        Guid UploadedByUserId,
        DateOnly UploadDate,
        bool Active,
        bool GeneratedByAI,
        Guid? ReviewedByUserId,
        DateOnly? ReviewedAt,
        bool? Approved);
}
