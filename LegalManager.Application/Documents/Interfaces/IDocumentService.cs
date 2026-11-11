using LegalManager.Application.Auth.DTOs;
using LegalManager.Application.Users.DTOs;
using LegalManager.Application.Cases.DTOs;
using LegalManager.Application.Appointments.DTOs;
using LegalManager.Application.Documents.DTOs;
using LegalManager.Application.Common;

namespace LegalManager.Application.Documents.Interfaces
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
