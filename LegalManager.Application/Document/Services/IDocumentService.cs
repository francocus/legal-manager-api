using LegalManager.Application.DTOs;
using LegalManager.Domain.Entities;

namespace LegalManager.Application.Interfaces
{
    public interface IDocumentService
    {
        Document Upload(UploadDocumentRequest request);

        Document GenerateAiSummary(Guid caseId, Guid generatedByUserId);

        IReadOnlyList<Document> GetByCaseId(Guid caseId);

        Document? GetById(Guid id);

        (Stream Stream, string ContentType, string FileName)? Download(Guid id);

        Document? Review(Guid documentId, Guid reviewedByUserId);

        bool Delete(Guid id);
    }
}
