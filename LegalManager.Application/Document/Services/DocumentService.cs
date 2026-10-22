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
        string rootStoragePath) : IDocumentService
    {
        public DocumentResponse Upload(UploadDocumentRequest request)
        {
            var caseItem = casesRepository.GetById(request.CaseId);
            if (caseItem == null)
                throw new ArgumentException("El expediente indicado no existe.");

            if (caseItem.Status == CaseStatus.Cerrado)
                throw new InvalidOperationException("No se pueden agregar documentos a un expediente cerrado.");

            EnsureCanWrite(caseItem);

            if (request.FileContent == null || request.Length == 0)
                throw new ArgumentException("Debe adjuntarse un archivo.");

            var caseFolder = Path.Combine(rootStoragePath, request.CaseId.ToString());
            Directory.CreateDirectory(caseFolder);

            var documentId = Guid.NewGuid();
            var storedFileName = $"{documentId}_{request.FileName}";
            var fullPath = Path.Combine(caseFolder, storedFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                request.FileContent.CopyTo(stream);
            }

            var document = new Document(request.CaseId, request.FileName, fullPath, request.ContentType, request.Length, request.Type, currentUser.Id);
            documentsRepository.Add(document);
            documentsRepository.Save();
            return DocumentResponse.Desde(document);
        }

        public IReadOnlyList<DocumentResponse> GetByCaseId(Guid caseId)
        {
            var caseItem = casesRepository.GetById(caseId);
            if (caseItem != null)
                EnsureCanRead(caseItem);

            return documentsRepository.GetByCaseId(caseId)
                .Where(document => !IsHiddenFromClient(document))
                .Select(DocumentResponse.Desde)
                .ToList();
        }

        public DocumentResponse? GetById(Guid id)
        {
            var document = documentsRepository.GetById(id);
            if (document == null) return null;

            if (IsHiddenFromClient(document)) return null;

            EnsureDocumentAccess(document);

            return DocumentResponse.Desde(document);
        }

        public (Stream Stream, string ContentType, string FileName)? Download(Guid id)
        {
            var document = documentsRepository.GetById(id);
            if (document == null) return null;

            if (IsHiddenFromClient(document)) return null;

            EnsureDocumentAccess(document);

            if (!File.Exists(document.FilePath))
                throw new InvalidOperationException("El archivo del documento no se encuentra en el servidor.");

            var stream = new FileStream(document.FilePath, FileMode.Open, FileAccess.Read);
            return (stream, document.ContentType, document.FileName);
        }

        public async Task<DocumentResponse> GenerateAiSummaryAsync(Guid caseId, CancellationToken cancellationToken = default)
        {
            var caseItem = casesRepository.GetById(caseId);
            if (caseItem == null)
                throw new ArgumentException("El expediente indicado no existe.");

            EnsureCanWrite(caseItem);

            // Quota guard: while a draft awaits review, generating another one would burn
            // LLM plan quota for nothing. Approve or discard the pending one first.
            if (documentsRepository.GetByCaseId(caseId).Any(d => d.Active && d.IsPendingReview))
                throw new InvalidOperationException("Ya existe un borrador de resumen IA pendiente de revisión. Aprobalo o descartalo antes de generar otro.");

            var caseContext = ComposeSummaryText(caseItem);
            var summary = await aiSummaryService.GenerateSummaryAsync(caseContext, cancellationToken);

            var caseFolder = Path.Combine(rootStoragePath, caseId.ToString());
            Directory.CreateDirectory(caseFolder);

            var documentId = Guid.NewGuid();
            var fileName = "ResumenIA.pdf";
            var storedFileName = $"{documentId}_{fileName}";
            var fullPath = Path.Combine(caseFolder, storedFileName);
            await File.WriteAllTextAsync(fullPath, summary, cancellationToken);

            var document = Document.CreateAiSummary(caseId, fileName, fullPath, summary, currentUser.Id);
            documentsRepository.Add(document);
            try
            {
                await documentsRepository.SaveAsync(cancellationToken);
            }
            catch
            {
                // Best-effort orphan cleanup: without it, a failed save leaves the written
                // file on disk without its database record.
                try { File.Delete(fullPath); } catch (IOException) { }
                throw;
            }
            return DocumentResponse.Desde(document);
        }

        public DocumentResponse? Approve(Guid documentId)
        {
            var document = documentsRepository.GetById(documentId);
            if (document == null) return null;

            EnsureDocumentWrite(document);

            document.Approve(currentUser.Id);
            documentsRepository.Save();
            return DocumentResponse.Desde(document);
        }

        public DocumentResponse? Discard(Guid documentId)
        {
            var document = documentsRepository.GetById(documentId);
            if (document == null) return null;

            EnsureDocumentWrite(document);

            document.Discard(currentUser.Id);
            documentsRepository.Save();
            return DocumentResponse.Desde(document);
        }

        public bool Delete(Guid id)
        {
            var document = documentsRepository.GetById(id);
            if (document == null) return false;

            EnsureDocumentWrite(document);

            document.Deactivate();
            documentsRepository.Save();
            return true;
        }

        private string ComposeSummaryText(Case caseItem)
        {
            // Case context handed to the LLM: built exclusively from data already registered
            // on the case, so the prompt can enforce a no-invented-facts summary.
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

        private void EnsureDocumentAccess(Document document)
        {
            var caseItem = casesRepository.GetById(document.CaseId);
            if (caseItem == null) return;

            EnsureCanRead(caseItem);
        }

        // An AI draft has not been reviewed by any lawyer yet: the client must not see
        // or download anything the firm has not approved. Filtered silently, without
        // throwing 403, because a 403 would confirm the document exists.
        private bool IsHiddenFromClient(Document document)
            => currentUser.IsClient && document.IsPendingReview;

        private void EnsureDocumentWrite(Document document)
        {
            var caseItem = casesRepository.GetById(document.CaseId);
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