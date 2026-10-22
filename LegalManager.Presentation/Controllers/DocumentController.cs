using LegalManager.Application.DTOs;
using LegalManager.Application.Interfaces;
using LegalManager.Domain;
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
        public async Task<ActionResult<DocumentResponse>> Upload([FromForm] Guid caseId, [FromForm] DocumentType type, IFormFile file, CancellationToken cancellationToken)
        {
            try
            {
                await using var stream = file.OpenReadStream();
                var request = new UploadDocumentRequest
                {
                    CaseId = caseId,
                    Type = type,
                    FileContent = stream,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Length = file.Length
                };

                var document = await documentService.UploadAsync(request, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = document.Id }, document);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpGet("case/{caseId}")]
        public async Task<ActionResult<IReadOnlyList<DocumentResponse>>> GetByCaseId([FromRoute] Guid caseId, CancellationToken cancellationToken)
            => Ok(await documentService.GetByCaseIdAsync(caseId, cancellationToken));

        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentResponse>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var document = await documentService.GetByIdAsync(id, cancellationToken);
            if (document == null) return NotFound($"No existe un documento con el id {id}.");
            return Ok(document);
        }

        [HttpGet("{id}/download")]
        public async Task<ActionResult> Download([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var result = await documentService.DownloadAsync(id, cancellationToken);
                if (result == null) return NotFound($"No existe un documento con el id {id}.");
                return File(result.Value.Stream, result.Value.ContentType, result.Value.FileName);
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpPost("case/{caseId}/generate-summary")]
        public async Task<ActionResult<DocumentResponse>> GenerateAiSummary([FromRoute] Guid caseId, CancellationToken cancellationToken)
        {
            try
            {
                var document = await documentService.GenerateAiSummaryAsync(caseId, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = document.Id }, document);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
            // The LLM failure is upstream of this API, so it maps to 502 at this action,
            // following the same per-action error handling used across the controller and
            // leaving every other exception handling untouched.
            catch (AiSummaryUnavailableException ex) { return StatusCode(StatusCodes.Status502BadGateway, ex.Message); }
        }

        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpPatch("{id}/approve")]
        public async Task<ActionResult<DocumentResponse>> Approve([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var document = await documentService.ApproveAsync(id, cancellationToken);
                if (document == null) return NotFound($"No existe un documento con el id {id}.");
                return Ok(document);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpPatch("{id}/discard")]
        public async Task<ActionResult<DocumentResponse>> Discard([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var document = await documentService.DiscardAsync(id, cancellationToken);
                if (document == null) return NotFound($"No existe un documento con el id {id}.");
                return Ok(document);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            if (!await documentService.DeleteAsync(id, cancellationToken))
                return NotFound($"No existe un documento con el id {id}.");
            return NoContent();
        }
    }
}