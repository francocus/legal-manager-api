using LegalManager.Application.DTOs;

namespace LegalManager.Application.Interfaces
{
    public interface IDocumentService
    {
        DocumentResponse Upload(UploadDocumentRequest request);

        DocumentResponse GenerateAiSummary(Guid caseId);

        IReadOnlyList<DocumentResponse> GetByCaseId(Guid caseId);

        DocumentResponse? GetById(Guid id);

        (Stream Stream, string ContentType, string FileName)? Download(Guid id);

        DocumentResponse? Approve(Guid documentId);

        DocumentResponse? Discard(Guid documentId);

        bool Delete(Guid id);
    }
}