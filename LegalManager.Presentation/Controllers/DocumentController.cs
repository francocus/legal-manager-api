using LegalManager.Application.DTOs;
using LegalManager.Application.Interfaces;
using LegalManager.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegalManager.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Policies.AllRoles)]
    public class DocumentController(IDocumentService documentService) : ControllerBase
    {
        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public ActionResult<DocumentResponse> Upload([FromForm] Guid caseId, [FromForm] DocumentType type, IFormFile file)
        {
            try
            {
                using var stream = file.OpenReadStream();
                var request = new UploadDocumentRequest
                {
                    CaseId = caseId,
                    Type = type,
                    FileContent = stream,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Length = file.Length
                };

                var document = documentService.Upload(request);
                return CreatedAtAction(nameof(GetById), new { id = document.Id }, document);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpGet("case/{caseId}")]
        public ActionResult<IReadOnlyList<DocumentResponse>> GetByCaseId([FromRoute] Guid caseId)
            => Ok(documentService.GetByCaseId(caseId));

        [HttpGet("{id}")]
        public ActionResult<DocumentResponse> GetById([FromRoute] Guid id)
        {
            var document = documentService.GetById(id);
            if (document == null) return NotFound($"No existe un documento con el id {id}.");
            return Ok(document);
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

        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpPost("case/{caseId}/generate-summary")]
        public ActionResult<DocumentResponse> GenerateAiSummary([FromRoute] Guid caseId)
        {
            try
            {
                var document = documentService.GenerateAiSummary(caseId);
                return CreatedAtAction(nameof(GetById), new { id = document.Id }, document);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpPatch("{id}/approve")]
        public ActionResult<DocumentResponse> Approve([FromRoute] Guid id)
        {
            try
            {
                var document = documentService.Approve(id);
                if (document == null) return NotFound($"No existe un documento con el id {id}.");
                return Ok(document);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpPatch("{id}/discard")]
        public ActionResult<DocumentResponse> Discard([FromRoute] Guid id)
        {
            try
            {
                var document = documentService.Discard(id);
                if (document == null) return NotFound($"No existe un documento con el id {id}.");
                return Ok(document);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpDelete("{id}")]
        public ActionResult Delete([FromRoute] Guid id)
        {
            if (!documentService.Delete(id))
                return NotFound($"No existe un documento con el id {id}.");
            return NoContent();
        }
    }
}