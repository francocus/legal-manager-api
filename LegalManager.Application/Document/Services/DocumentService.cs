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
