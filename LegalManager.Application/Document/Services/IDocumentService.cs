using LegalManager.Application.DTOs;

namespace LegalManager.Application.Interfaces
{
    public interface IDocumentService
    {
        DocumentResponse Upload(UploadDocumentRequest request);

        DocumentResponse GenerateAiSummary(Guid caseId, Guid generatedByUserId);

        IReadOnlyList<DocumentResponse> GetByCaseId(Guid caseId);

        DocumentResponse? GetById(Guid id);

        (Stream Stream, string ContentType, string FileName)? Download(Guid id);

        DocumentResponse? Approve(Guid documentId, Guid reviewedByUserId);

        DocumentResponse? Discard(Guid documentId, Guid reviewedByUserId);

        bool Delete(Guid id);
    }
}