using LegalManager.Application.DTOs;
using LegalManager.Application.Interfaces;
using LegalManager.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace LegalManager.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentController : ControllerBase
    {
        private readonly IAppointmentService appointmentService;

        public AppointmentController(IAppointmentService appointmentService)
        {
            this.appointmentService = appointmentService;
        }

        private static AppointmentResponse ToResponse(Appointment a) => new(
            a.Id, a.Title, a.Date, a.Time, a.EndTime, a.Reason, a.Status, a.EffectiveStatus,
            a.Area, a.Location, a.Notes, a.ClientId, a.LawyerId, a.CaseId, a.Active);

        [HttpPost]
        public ActionResult<AppointmentResponse> Create([FromBody] CreateAppointmentRequest request)
        {
            try
            {
                var appointment = appointmentService.Create(request);
                return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, ToResponse(appointment));
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpGet]
        public ActionResult<IReadOnlyList<AppointmentResponse>> GetAll()
        {
            var appointments = appointmentService.GetAll();
            if (!appointments.Any()) return NotFound("No hay elementos en la lista.");
            return Ok(appointments.Select(ToResponse).ToList());
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
            return Ok(ToResponse(appointment));
        }

        [HttpPatch("{id}/confirm")]
        public ActionResult<AppointmentResponse> Confirm([FromRoute] Guid id)
        {
            try
            {
                var appointment = appointmentService.Confirm(id);
                if (appointment == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(ToResponse(appointment));
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
                return Ok(ToResponse(appointment));
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
                return Ok(ToResponse(appointment));
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

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