using LegalManager.Application.Auth.DTOs;
using LegalManager.Application.Users.DTOs;
using LegalManager.Application.Cases.DTOs;
using LegalManager.Application.Appointments.DTOs;
using LegalManager.Application.Documents.DTOs;
using LegalManager.Application.Common;
using LegalManager.Application.Auth.Interfaces;
using LegalManager.Application.Users.Interfaces;
using LegalManager.Application.Cases.Interfaces;
using LegalManager.Application.Appointments.Interfaces;
using LegalManager.Application.Documents.Interfaces;
using LegalManager.Application.Common.Interfaces;
using LegalManager.Domain.Entities;
using LegalManager.Domain.Enums;
using LegalManager.Domain.Interfaces;
using LegalManager.Application.Common.Exceptions;

namespace LegalManager.Application.Documents.Services
{
    public class DocumentService(
        IDocumentRepository documentsRepository,
        ICaseRepository casesRepository,
        IUserRepository usersRepository,
        IAiSummaryService aiSummaryService,
        IPdfDocumentRenderer pdfRenderer,
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

        public async Task<GeneratedDocumentDraftResponse> GenerateAiSummaryAsync(Guid caseId, CancellationToken cancellationToken = default)
        {
            var caseItem = await casesRepository.GetByIdAsync(caseId, cancellationToken);
            if (caseItem == null)
                throw new ArgumentException("El expediente indicado no existe.");

            EnsureCanWrite(caseItem);

            var caseContext = ComposeSummaryText(caseItem);
            var summary = await aiSummaryService.GenerateSummaryAsync(caseContext, cancellationToken);

            var pdfBytes = pdfRenderer.RenderSummaryPdf(new SummaryPdfData(
                caseItem.CaseNumber,
                caseItem.Title,
                caseItem.Area,
                caseItem.Status.ToString(),
                caseItem.StartDate.ToString("dd/MM/yyyy"),
                caseItem.LastUpdate.ToString("dd/MM/yyyy"),
                caseItem.ClosingDate?.ToString("dd/MM/yyyy"),
                caseItem.Description ?? string.Empty,
                caseItem.Notes ?? string.Empty,
                summary));

            return new GeneratedDocumentDraftResponse(
                "ResumenIA.pdf",
                "application/pdf",
                pdfBytes.Length,
                summary,
                Convert.ToBase64String(pdfBytes));
        }

        public async Task<GeneratedDocumentDraftResponse> GenerateConstanciaAsync(Guid caseId, CancellationToken cancellationToken = default)
        {
            var caseItem = await casesRepository.GetByIdAsync(caseId, cancellationToken);
            if (caseItem == null)
                throw new ArgumentException("El expediente indicado no existe.");

            EnsureCanWrite(caseItem);

            var pdfBytes = pdfRenderer.RenderConstanciaPdf(new ConstanciaPdfData(
                caseItem.CaseNumber,
                caseItem.Title,
                await GetClientDisplayNameAsync(caseItem.ClientId, cancellationToken),
                await GetLawyerNamesAsync(caseItem, cancellationToken),
                caseItem.Area,
                caseItem.Status.ToString(),
                caseItem.StartDate.ToString("dd/MM/yyyy"),
                caseItem.LastUpdate.ToString("dd/MM/yyyy"),
                caseItem.Description ?? string.Empty,
                DateTime.Now.ToString("dd/MM/yyyy HH:mm")));

            return new GeneratedDocumentDraftResponse(
                "Constancia.pdf",
                "application/pdf",
                pdfBytes.Length,
                string.Empty,
                Convert.ToBase64String(pdfBytes));
        }

        public async Task<DocumentResponse> SaveGeneratedAsync(SaveGeneratedDocumentRequest request, CancellationToken cancellationToken = default)
        {
            var caseItem = await casesRepository.GetByIdAsync(request.CaseId, cancellationToken);
            if (caseItem == null)
                throw new ArgumentException("El expediente indicado no existe.");

            if (caseItem.Status == CaseStatus.Cerrado)
                throw new InvalidOperationException("No se pueden agregar documentos a un expediente cerrado.");

            EnsureCanWrite(caseItem);

            if (string.IsNullOrEmpty(request.PdfBase64))
                throw new ArgumentException("El contenido del documento es obligatorio.");

            byte[] pdfBytes;
            try
            {
                pdfBytes = Convert.FromBase64String(request.PdfBase64);
            }
            catch (FormatException)
            {
                throw new ArgumentException("El contenido del documento es inválido.");
            }

            if (!pdfBytes.AsSpan().StartsWith("%PDF-"u8))
                throw new ArgumentException("Todo documento generado debe ser un PDF válido.");

            if (pdfBytes.Length > 10 * 1024 * 1024)
                throw new ArgumentException("El archivo supera el tamaño máximo permitido (10 MB).");

            var fileName = request.GeneratedByAi ? "ResumenIA.pdf" : "Constancia.pdf";

            var storagePath = await fileStorage.SaveAsync(
                request.CaseId,
                fileName,
                new MemoryStream(pdfBytes),
                "application/pdf",
                cancellationToken);

            Document document;
            if (request.GeneratedByAi)
            {
                document = Document.CreateAiSummary(request.CaseId, fileName, storagePath, request.SummaryText, currentUser.Id);
                document.Approve(currentUser.Id);
            }
            else
            {
                document = new Document(request.CaseId, fileName, storagePath, "application/pdf", pdfBytes.Length, DocumentType.Generado, currentUser.Id);
            }

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

        private async Task<string> GetClientDisplayNameAsync(Guid clientId, CancellationToken cancellationToken)
        {
            var client = await usersRepository.GetByIdAsync(clientId, cancellationToken);
            if (client == null) return string.Empty;

            return string.IsNullOrWhiteSpace(client.FullName) ? client.Email : client.FullName;
        }

        private async Task<string> GetLawyerNamesAsync(Case caseItem, CancellationToken cancellationToken)
        {
            var names = new List<string>();
            foreach (var lawyerId in caseItem.LawyerIds)
            {
                var lawyer = await usersRepository.GetByIdAsync(lawyerId, cancellationToken);
                if (lawyer != null)
                    names.Add(lawyer.FullName);
            }

            return string.Join(", ", names);
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