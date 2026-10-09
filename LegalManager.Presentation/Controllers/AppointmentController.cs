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
        public ActionResult<AppointmentResponse> Create([FromBody] CreateAppointmentRequest request)
        {
            try
            {
                var appointment = appointmentService.Create(request);
                return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpGet]
        public ActionResult<IReadOnlyList<AppointmentResponse>> GetAll()
        {
            var appointments = appointmentService.GetAll();
            return Ok(appointments);
        }

        [HttpGet("availability")]
        public ActionResult<AvailabilityResponse> GetAvailability([FromQuery] Guid lawyerId, [FromQuery] DateOnly date)
        {
            try
            {
                return Ok(new AvailabilityResponse(appointmentService.GetAvailability(lawyerId, date)));
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("{id}")]
        public ActionResult<AppointmentResponse> GetById([FromRoute] Guid id)
        {
            var appointment = appointmentService.GetById(id);
            if (appointment == null) return NotFound($"No existe un elemento con el id {id}.");
            return Ok(appointment);
        }

        [HttpPatch("{id}/confirm")]
        public ActionResult<AppointmentResponse> Confirm([FromRoute] Guid id)
        {
            try
            {
                var appointment = appointmentService.Confirm(id);
                if (appointment == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(appointment);
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpPatch("{id}/cancel")]
        public ActionResult<AppointmentResponse> Cancel([FromRoute] Guid id)
        {
            try
            {
                var appointment = appointmentService.Cancel(id);
                if (appointment == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(appointment);
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpPatch("{id}/reschedule")]
        public ActionResult<AppointmentResponse> Reschedule([FromRoute] Guid id, [FromBody] RescheduleAppointmentRequest request)
        {
            try
            {
                var appointment = appointmentService.Reschedule(id, request);
                if (appointment == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(appointment);
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminsOnly)]
        [HttpDelete("{id}")]
        public ActionResult Delete([FromRoute] Guid id)
        {
            try
            {
                if (!appointmentService.Delete(id))
                    return NotFound($"No existe un elemento con el id {id}.");
                return NoContent();
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }
    }
}