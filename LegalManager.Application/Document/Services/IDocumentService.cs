using LegalManager.Application.DTOs;

namespace LegalManager.Application.Interfaces
{
    public interface IDocumentService
    {
        Task<DocumentResponse> UploadAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default);

        Task<DocumentResponse> GenerateAiSummaryAsync(Guid caseId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<DocumentResponse>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default);

        Task<DocumentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<(Stream Stream, string ContentType, string FileName)?> DownloadAsync(Guid id, CancellationToken cancellationToken = default);

        Task<DocumentResponse?> ApproveAsync(Guid documentId, CancellationToken cancellationToken = default);

        Task<DocumentResponse?> DiscardAsync(Guid documentId, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}