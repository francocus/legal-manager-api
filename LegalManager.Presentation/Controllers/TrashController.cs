using LegalManager.Presentation.Auth;
using LegalManager.Application.Trash.DTOs;
using LegalManager.Application.Trash.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegalManager.Presentation.Controllers
{
    // Restore-only trash (papelera): no hard delete or purge exists anywhere.
    [Route("api/trash")]
    [ApiController]
    [Authorize(Policy = Policies.AdminOrLawyer)]
    public class TrashController(ITrashService trashService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<TrashResponse>> GetTrash(CancellationToken cancellationToken)
            => Ok(await trashService.GetTrashAsync(cancellationToken));

        [HttpPost("case/{id}/restore")]
        public async Task<ActionResult> RestoreCase([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            try
            {
                if (!await trashService.RestoreCaseAsync(id, cancellationToken))
                    return NotFound($"No existe un elemento con el id {id}.");
                return NoContent();
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpPost("appointment/{id}/restore")]
        public async Task<ActionResult> RestoreAppointment([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            try
            {
                if (!await trashService.RestoreAppointmentAsync(id, cancellationToken))
                    return NotFound($"No existe un elemento con el id {id}.");
                return NoContent();
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpPost("document/{id}/restore")]
        public async Task<ActionResult> RestoreDocument([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            try
            {
                if (!await trashService.RestoreDocumentAsync(id, cancellationToken))
                    return NotFound($"No existe un elemento con el id {id}.");
                return NoContent();
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpPost("user/{id}/restore")]
        public async Task<ActionResult> RestoreUser([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            try
            {
                if (!await trashService.RestoreUserAsync(id, cancellationToken))
                    return NotFound($"No existe un elemento con el id {id}.");
                return NoContent();
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }
    }
}
