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
        bool? Approved,
        bool IsPendingReview)
    {
        public static DocumentResponse Desde(Document d) => new(
            d.Id, d.CaseId, d.FileName, d.ContentType, d.SizeBytes, d.Type, d.UploadedByUserId, d.UploadDate, d.Active, d.GeneratedByAI, d.ReviewedByUserId, d.ReviewedAt, d.Approved, d.IsPendingReview);
    }
}
