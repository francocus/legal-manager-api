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
        public ActionResult<CaseResponse> Create([FromBody] CreateCaseRequest request)
        {
            try
            {
                var caseItem = caseService.Create(request);
                return CreatedAtAction(nameof(GetById), new { id = caseItem.Id }, caseItem);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpGet]
        public ActionResult<IReadOnlyList<CaseResponse>> GetAll()
        {
            var cases = caseService.GetAll();
            return Ok(cases);
        }

        [HttpGet("{id}")]
        public ActionResult<CaseResponse> GetById([FromRoute] Guid id)
        {
            var caseItem = caseService.GetById(id);
            if (caseItem == null) return NotFound($"No existe un elemento con el id {id}.");
            return Ok(caseItem);
        }

        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpPut("{id}")]
        public ActionResult<CaseResponse> Update([FromRoute] Guid id, [FromBody] UpdateCaseRequest request)
        {
            try
            {
                var caseItem = caseService.Update(id, request);
                if (caseItem == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(caseItem);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpPatch("{id}/status")]
        public ActionResult<CaseResponse> ChangeStatus([FromRoute] Guid id, [FromBody] ChangeStatusRequest request)
        {
            try
            {
                var caseItem = caseService.ChangeStatus(id, request);
                if (caseItem == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(caseItem);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpPatch("{id}/lawyers/add")]
        public ActionResult<CaseResponse> AddLawyer([FromRoute] Guid id, [FromBody] AddLawyerRequest request)
        {
            try
            {
                var caseItem = caseService.AddLawyer(id, request);
                if (caseItem == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(caseItem);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpPatch("{id}/lawyers/remove")]
        public ActionResult<CaseResponse> RemoveLawyer([FromRoute] Guid id, [FromBody] RemoveLawyerRequest request)
        {
            try
            {
                var caseItem = caseService.RemoveLawyer(id, request);
                if (caseItem == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(caseItem);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminsOnly)]
        [HttpDelete("{id}")]
        public ActionResult Delete([FromRoute] Guid id)
        {
            try
            {
                if (!caseService.Delete(id))
                    return NotFound($"No existe un elemento con el id {id}.");
                return NoContent();
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }
    }
}