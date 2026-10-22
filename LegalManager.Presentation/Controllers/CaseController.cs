using LegalManager.Application.DTOs;
using LegalManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegalManager.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Policies.AllRoles)]
    public class CaseController(ICaseService caseService) : ControllerBase
    {
        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpPost]
        public async Task<ActionResult<CaseResponse>> Create([FromBody] CreateCaseRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var caseItem = await caseService.CreateAsync(request, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = caseItem.Id }, caseItem);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CaseResponse>>> GetAll(CancellationToken cancellationToken)
        {
            var cases = await caseService.GetAllAsync(cancellationToken);
            return Ok(cases);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CaseResponse>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var caseItem = await caseService.GetByIdAsync(id, cancellationToken);
            if (caseItem == null) return NotFound($"No existe un elemento con el id {id}.");
            return Ok(caseItem);
        }

        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpPut("{id}")]
        public async Task<ActionResult<CaseResponse>> Update([FromRoute] Guid id, [FromBody] UpdateCaseRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var caseItem = await caseService.UpdateAsync(id, request, cancellationToken);
                if (caseItem == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(caseItem);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpPatch("{id}/status")]
        public async Task<ActionResult<CaseResponse>> ChangeStatus([FromRoute] Guid id, [FromBody] ChangeStatusRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var caseItem = await caseService.ChangeStatusAsync(id, request, cancellationToken);
                if (caseItem == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(caseItem);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpPatch("{id}/lawyers/add")]
        public async Task<ActionResult<CaseResponse>> AddLawyer([FromRoute] Guid id, [FromBody] AddLawyerRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var caseItem = await caseService.AddLawyerAsync(id, request, cancellationToken);
                if (caseItem == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(caseItem);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpPatch("{id}/lawyers/remove")]
        public async Task<ActionResult<CaseResponse>> RemoveLawyer([FromRoute] Guid id, [FromBody] RemoveLawyerRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var caseItem = await caseService.RemoveLawyerAsync(id, request, cancellationToken);
                if (caseItem == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(caseItem);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminsOnly)]
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            try
            {
                if (!await caseService.DeleteAsync(id, cancellationToken))
                    return NotFound($"No existe un elemento con el id {id}.");
                return NoContent();
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }
    }
}