using LegalManager.Application.DTOs;
using LegalManager.Application.Interfaces;
using LegalManager.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace LegalManager.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService documentService;

        public DocumentController(IDocumentService documentService)
        {
            this.documentService = documentService;
        }

        private static DocumentResponse ToResponse(Document d) => new(
            d.Id, d.CaseId, d.FileName, d.ContentType, d.SizeBytes, d.Type, d.UploadedByUserId, d.UploadDate, d.Active);

        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public ActionResult<DocumentResponse> Upload([FromForm] Guid caseId, [FromForm] DocumentType type, [FromForm] Guid uploadedByUserId, IFormFile file)
        {
            try
            {
                using var stream = file.OpenReadStream();
                var request = new UploadDocumentRequest
                {
                    CaseId = caseId,
                    Type = type,
                    UploadedByUserId = uploadedByUserId,
                    FileContent = stream,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Length = file.Length
                };

                var document = documentService.Upload(request);
                return CreatedAtAction(nameof(GetById), new { id = document.Id }, ToResponse(document));
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpGet("case/{caseId}")]
        public ActionResult<IReadOnlyList<DocumentResponse>> GetByCaseId([FromRoute] Guid caseId)
            => Ok(documentService.GetByCaseId(caseId).Select(ToResponse).ToList());

        [HttpGet("{id}")]
        public ActionResult<DocumentResponse> GetById([FromRoute] Guid id)
        {
            var document = documentService.GetById(id);
            if (document == null) return NotFound($"No existe un documento con el id {id}.");
            return Ok(ToResponse(document));
        }

        [HttpGet("{id}/download")]
        public ActionResult Download([FromRoute] Guid id)
        {
            try
            {
                var result = documentService.Download(id);
                if (result == null) return NotFound($"No existe un documento con el id {id}.");
                return File(result.Value.Stream, result.Value.ContentType, result.Value.FileName);
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpDelete("{id}")]
        public ActionResult Delete([FromRoute] Guid id)
        {
            if (!documentService.Delete(id))
                return NotFound($"No existe un documento con el id {id}.");
            return NoContent();
        }
    }
}
