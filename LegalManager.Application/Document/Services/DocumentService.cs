using System.Text;
using LegalManager.Application.DTOs;
using LegalManager.Application.Interfaces;
using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;

namespace LegalManager.Application.Services
{
    public class DocumentService(
        IDocumentRepository documentsRepository,
        ICaseRepository casesRepository,
        IAiSummaryService aiSummaryService,
        ICurrentUser currentUser,
        IFileStorage fileStorage) : IDocumentService
    {
        public async Task<DocumentResponse> UploadAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default)
        {
            var caseItem = await casesRepository.GetByIdAsync(request.CaseId, cancellationToken);
            if (caseItem == null)
                throw new ArgumentException("El expediente indicado no existe.");

            if (caseItem.Status == CaseStatus.Cerrado)
                throw new InvalidOperationException("No se pueden agregar documentos a un expediente cerrado.");

            EnsureCanWrite(caseItem);

            if (request.FileContent == null || request.Length == 0)
                throw new ArgumentException("Debe adjuntarse un archivo.");

            var storagePath = await fileStorage.SaveAsync(
                request.CaseId,
                request.FileName,
                request.FileContent,
                request.ContentType,
                cancellationToken);

            var document = new Document(request.CaseId, request.FileName, storagePath, request.ContentType, request.Length, request.Type, currentUser.Id);
            documentsRepository.Add(document);
            try
            {
                await documentsRepository.SaveAsync(cancellationToken);
            }
            catch
            {
                await fileStorage.DeleteAsync(storagePath, cancellationToken);
                throw;
            }

            return DocumentResponse.Desde(document);
        }

        public async Task<IReadOnlyList<DocumentResponse>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default)
        {
            var caseItem = await casesRepository.GetByIdAsync(caseId, cancellationToken);
            if (caseItem != null)
                EnsureCanRead(caseItem);

            var documents = await documentsRepository.GetByCaseIdAsync(caseId, cancellationToken);
            return documents
                .Where(document => !IsHiddenFromClient(document))
                .Select(DocumentResponse.Desde)
                .ToList();
        }

        public async Task<DocumentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var document = await documentsRepository.GetByIdAsync(id, cancellationToken);
            if (document == null) return null;

            if (IsHiddenFromClient(document)) return null;

            await EnsureDocumentAccessAsync(document, cancellationToken);

            return DocumentResponse.Desde(document);
        }

        public async Task<(Stream Stream, string ContentType, string FileName)?> DownloadAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var document = await documentsRepository.GetByIdAsync(id, cancellationToken);
            if (document == null) return null;

            if (IsHiddenFromClient(document)) return null;

            await EnsureDocumentAccessAsync(document, cancellationToken);

            if (!await fileStorage.ExistsAsync(document.FilePath, cancellationToken))
                throw new InvalidOperationException("El archivo del documento no se encuentra en el almacenamiento.");

            var stream = await fileStorage.OpenReadAsync(document.FilePath, cancellationToken);
            return (stream, document.ContentType, document.FileName);
        }

        public async Task<DocumentResponse> GenerateAiSummaryAsync(Guid caseId, CancellationToken cancellationToken = default)
        {
            var caseItem = await casesRepository.GetByIdAsync(caseId, cancellationToken);
            if (caseItem == null)
                throw new ArgumentException("El expediente indicado no existe.");

            EnsureCanWrite(caseItem);

            // Quota guard: while a draft awaits review, generating another one would burn
            // LLM plan quota for nothing. Approve or discard the pending one first.
            var existingDocs = await documentsRepository.GetByCaseIdAsync(caseId, cancellationToken);
            if (existingDocs.Any(d => d.Active && d.IsPendingReview))
                throw new InvalidOperationException("Ya existe un borrador de resumen IA pendiente de revisión. Aprobalo o descartalo antes de generar otro.");

            var caseContext = ComposeSummaryText(caseItem);
            var summary = await aiSummaryService.GenerateSummaryAsync(caseContext, cancellationToken);

            var fileName = "ResumenIA.pdf";
            var summaryBytes = Encoding.UTF8.GetBytes(summary);
            using var summaryStream = new MemoryStream(summaryBytes);

            var storagePath = await fileStorage.SaveAsync(
                caseId,
                fileName,
                summaryStream,
                "application/pdf",
                cancellationToken);

            var document = Document.CreateAiSummary(caseId, fileName, storagePath, summary, currentUser.Id);
            documentsRepository.Add(document);
            try
            {
                await documentsRepository.SaveAsync(cancellationToken);
            }
            catch
            {
                // Best-effort orphan cleanup: without it, a failed save leaves the written
                // file on storage without its database record.
                await fileStorage.DeleteAsync(storagePath, cancellationToken);
                throw;
            }
            return DocumentResponse.Desde(document);
        }

        public async Task<DocumentResponse?> ApproveAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            var document = await documentsRepository.GetByIdAsync(documentId, cancellationToken);
            if (document == null) return null;

            await EnsureDocumentWriteAsync(document, cancellationToken);

            document.Approve(currentUser.Id);
            await documentsRepository.SaveAsync(cancellationToken);
            return DocumentResponse.Desde(document);
        }

        public async Task<DocumentResponse?> DiscardAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            var document = await documentsRepository.GetByIdAsync(documentId, cancellationToken);
            if (document == null) return null;

            await EnsureDocumentWriteAsync(document, cancellationToken);

            document.Discard(currentUser.Id);
            await documentsRepository.SaveAsync(cancellationToken);
            return DocumentResponse.Desde(document);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var document = await documentsRepository.GetByIdAsync(id, cancellationToken);
            if (document == null) return false;

            await EnsureDocumentWriteAsync(document, cancellationToken);

            document.Deactivate();
            await documentsRepository.SaveAsync(cancellationToken);
            return true;
        }

        private string ComposeSummaryText(Case caseItem)
        {
            var closing = caseItem.ClosingDate.HasValue
                ? $" Fue cerrado el {caseItem.ClosingDate:dd/MM/yyyy}."
                : string.Empty;
            var notes = string.IsNullOrWhiteSpace(caseItem.Notes)
                ? string.Empty
                : $" Notas: {caseItem.Notes}";

            return $"El expediente {caseItem.CaseNumber} - {caseItem.Title}, del área {caseItem.Area}, se encuentra en estado {caseItem.Status}." +
                   $" Iniciado el {caseItem.StartDate:dd/MM/yyyy}, su última actualización fue el {caseItem.LastUpdate:dd/MM/yyyy}.{closing}" +
                   $" Descripción: {caseItem.Description}.{notes}";
        }

        private async Task EnsureDocumentAccessAsync(Document document, CancellationToken cancellationToken = default)
        {
            var caseItem = await casesRepository.GetByIdAsync(document.CaseId, cancellationToken);
            if (caseItem == null) return;

            EnsureCanRead(caseItem);
        }

        private bool IsHiddenFromClient(Document document)
            => currentUser.IsClient && document.IsPendingReview;

        private async Task EnsureDocumentWriteAsync(Document document, CancellationToken cancellationToken = default)
        {
            var caseItem = await casesRepository.GetByIdAsync(document.CaseId, cancellationToken);
            if (caseItem == null)
                throw new ArgumentException("El expediente indicado no existe.");

            EnsureCanWrite(caseItem);
        }

        private void EnsureCanWrite(Case caseItem)
        {
            if (currentUser.IsAdmin) return;

            if (currentUser.IsLawyer && caseItem.LawyerIds.Contains(currentUser.Id)) return;

            throw new ForbiddenException("Solo el administrador o un abogado que gestiona el expediente pueden realizar esta operación.");
        }

        private void EnsureCanRead(Case caseItem)
        {
            if (currentUser.IsAdmin) return;

            if (currentUser.IsLawyer && caseItem.LawyerIds.Contains(currentUser.Id)) return;

            if (currentUser.IsClient && caseItem.ClientId == currentUser.Id) return;

            throw new ForbiddenException("No tiene acceso a este expediente.");
        }
    }
}
