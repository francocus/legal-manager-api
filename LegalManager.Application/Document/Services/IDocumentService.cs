using LegalManager.Application.DTOs;
using LegalManager.Domain.Entities;

namespace LegalManager.Application.Interfaces
{
    public interface IDocumentService
    {
        Document Upload(UploadDocumentRequest request);

        IReadOnlyList<Document> GetByCaseId(Guid caseId);

        Document? GetById(Guid id);

        (Stream Stream, string ContentType, string FileName)? Download(Guid id);

        bool Delete(Guid id);
    }
}
