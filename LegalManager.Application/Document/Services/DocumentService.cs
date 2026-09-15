using LegalManager.Application.DTOs;
using LegalManager.Application.Interfaces;
using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;

namespace LegalManager.Application.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository documentsRepository;
        private readonly ICaseRepository casesRepository;
        private readonly IUserRepository usersRepository;
        private readonly string rootStoragePath;

        public DocumentService(IDocumentRepository documentsRepository, ICaseRepository casesRepository, IUserRepository usersRepository, string rootStoragePath)
        {
            this.documentsRepository = documentsRepository;
            this.casesRepository = casesRepository;
            this.usersRepository = usersRepository;
            this.rootStoragePath = rootStoragePath;
        }

        public Document Upload(UploadDocumentRequest request)
        {
            var caseItem = casesRepository.GetById(request.CaseId);
            if (caseItem == null)
                throw new ArgumentException("El expediente indicado no existe.");

            if (caseItem.Status == CaseStatus.Cerrado)
                throw new InvalidOperationException("No se pueden agregar documentos a un expediente cerrado.");

            if (usersRepository.GetById(request.UploadedByUserId) == null)
                throw new ArgumentException("El usuario que sube el documento no es válido.");

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

            var document = new Document(request.CaseId, request.FileName, fullPath, request.ContentType, request.Length, request.Type, request.UploadedByUserId);
            documentsRepository.Add(document);
            documentsRepository.Save();
            return document;
        }

        public IReadOnlyList<Document> GetByCaseId(Guid caseId) => documentsRepository.GetByCaseId(caseId);

        public Document? GetById(Guid id) => documentsRepository.GetById(id);

        public (Stream Stream, string ContentType, string FileName)? Download(Guid id)
        {
            var document = GetById(id);
            if (document == null) return null;

            if (!File.Exists(document.FilePath))
                throw new InvalidOperationException("El archivo del documento no se encuentra en el servidor.");

            var stream = new FileStream(document.FilePath, FileMode.Open, FileAccess.Read);
            return (stream, document.ContentType, document.FileName);
        }

        public Document GenerateAiSummary(Guid caseId, Guid generatedByUserId)
        {
            var caseItem = casesRepository.GetById(caseId);
            if (caseItem == null)
                throw new ArgumentException("El expediente indicado no existe.");

            if (usersRepository.GetById(generatedByUserId) == null)
                throw new ArgumentException("El usuario que genera el resumen no es válido.");

            var summary = ComposeSummaryText(caseItem);

            var caseFolder = Path.Combine(rootStoragePath, caseId.ToString());
            Directory.CreateDirectory(caseFolder);

            var documentId = Guid.NewGuid();
            var fileName = "ResumenIA.pdf";
            var storedFileName = $"{documentId}_{fileName}";
            var fullPath = Path.Combine(caseFolder, storedFileName);
            File.WriteAllText(fullPath, summary);

            var document = Document.CreateAiSummary(caseId, fileName, fullPath, summary, generatedByUserId);
            documentsRepository.Add(document);
            documentsRepository.Save();
            return document;
        }

        public Document? Approve(Guid documentId, Guid reviewedByUserId)
        {
            var document = documentsRepository.GetById(documentId);
            if (document == null) return null;

            document.Approve(reviewedByUserId);
            documentsRepository.Save();
            return document;
        }

        public Document? Discard(Guid documentId, Guid reviewedByUserId)
        {
            var document = documentsRepository.GetById(documentId);
            if (document == null) return null;

            document.Discard(reviewedByUserId);
            documentsRepository.Save();
            return document;
        }

        private string ComposeSummaryText(Case caseItem)
        {
            // TODO: reemplazar por una llamada real a un proveedor de LLM (Gemini, GPT, etc.)
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

        public bool Delete(Guid id)
        {
            var document = GetById(id);
            if (document == null) return false;

            document.Deactivate();
            documentsRepository.Save();
            return true;
        }
    }
}
