using LegalManager.Application.DTOs;

namespace LegalManager.Application.Interfaces
{
    public interface IDocumentService
    {
        Task<DocumentResponse> UploadAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default);

        Task<GeneratedDocumentDraftResponse> GenerateAiSummaryAsync(Guid caseId, CancellationToken cancellationToken = default);

        Task<GeneratedDocumentDraftResponse> GenerateConstanciaAsync(Guid caseId, CancellationToken cancellationToken = default);

        Task<DocumentResponse> SaveGeneratedAsync(SaveGeneratedDocumentRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<DocumentResponse>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default);

        Task<DocumentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<(Stream Stream, string ContentType, string FileName)?> DownloadAsync(Guid id, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
