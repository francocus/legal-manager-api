using LegalManager.Application.DTOs;
using LegalManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegalManager.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Policies.AllRoles)]
    public class AppointmentController(IAppointmentService appointmentService) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<AppointmentResponse>> Create([FromBody] CreateAppointmentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var appointment = await appointmentService.CreateAsync(request, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AppointmentResponse>>> GetAll(CancellationToken cancellationToken)
        {
            var appointments = await appointmentService.GetAllAsync(cancellationToken);
            return Ok(appointments);
        }

        [HttpGet("availability")]
        public async Task<ActionResult<AvailabilityResponse>> GetAvailability([FromQuery] Guid lawyerId, [FromQuery] DateOnly date, CancellationToken cancellationToken)
        {
            try
            {
                var availableSlots = await appointmentService.GetAvailabilityAsync(lawyerId, date, cancellationToken);
                return Ok(new AvailabilityResponse(availableSlots));
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AppointmentResponse>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var appointment = await appointmentService.GetByIdAsync(id, cancellationToken);
            if (appointment == null) return NotFound($"No existe un elemento con el id {id}.");
            return Ok(appointment);
        }

        [HttpPatch("{id}/confirm")]
        public async Task<ActionResult<AppointmentResponse>> Confirm([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var appointment = await appointmentService.ConfirmAsync(id, cancellationToken);
                if (appointment == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(appointment);
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpPatch("{id}/cancel")]
        public async Task<ActionResult<AppointmentResponse>> Cancel([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var appointment = await appointmentService.CancelAsync(id, cancellationToken);
                if (appointment == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(appointment);
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpPatch("{id}/reschedule")]
        public async Task<ActionResult<AppointmentResponse>> Reschedule([FromRoute] Guid id, [FromBody] RescheduleAppointmentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var appointment = await appointmentService.RescheduleAsync(id, request, cancellationToken);
                if (appointment == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(appointment);
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
                if (!await appointmentService.DeleteAsync(id, cancellationToken))
                    return NotFound($"No existe un elemento con el id {id}.");
                return NoContent();
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }
    }
}