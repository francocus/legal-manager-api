using LegalManager.Application.DTOs;
using LegalManager.Application.Interfaces;
using LegalManager.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace LegalManager.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService userService;

        public UserController(IUserService userService)
        {
            this.userService = userService;
        }

        private static UserResponse ToResponse(User user)
        {
            var phone = user switch { Client c => c.Phone, Lawyer l => l.Phone, _ => null };
            var address = (user as Client)?.Address;
            return new(user.Id, user.FirstName, user.LastName, user.Dni, user.Email, user.RegistrationDate, user switch { Client => UserType.Client, Lawyer => UserType.Lawyer, Admin => UserType.Admin, _ => "unknown" }, phone, address);
        }

        [HttpPost("client")]
        public ActionResult<UserResponse> CreateClient([FromBody] CreateClientRequest request)
        {
            try
            {
                var client = userService.CreateClient(request);
                return CreatedAtAction(nameof(GetById), new { id = client.Id }, ToResponse(client));
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpPost("lawyer")]
        public ActionResult<UserResponse> CreateLawyer([FromBody] CreateLawyerRequest request)
        {
            try
            {
                var lawyer = userService.CreateLawyer(request);
                return CreatedAtAction(nameof(GetById), new { id = lawyer.Id }, ToResponse(lawyer));
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpPost("admin")]
        public ActionResult<UserResponse> CreateAdmin([FromBody] CreateAdminRequest request)
        {
            try
            {
                var admin = userService.CreateAdmin(request);
                return CreatedAtAction(nameof(GetById), new { id = admin.Id }, ToResponse(admin));
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpGet]
        public ActionResult<IReadOnlyList<UserResponse>> GetAll()
        {
            var users = userService.GetAll();
            if (!users.Any()) return NotFound("No hay elementos en la lista.");
            return Ok(users.Select(ToResponse).ToList());
        }

        [HttpGet("clients")]
        public ActionResult<IReadOnlyList<UserResponse>> GetClients()
            => Ok(userService.GetClients().Select(ToResponse).ToList());

        [HttpGet("lawyers")]
        public ActionResult<IReadOnlyList<UserResponse>> GetLawyers()
            => Ok(userService.GetLawyers().Select(ToResponse).ToList());

        [HttpGet("admins")]
        public ActionResult<IReadOnlyList<UserResponse>> GetAdmins()
            => Ok(userService.GetAdmins().Select(ToResponse).ToList());

        [HttpGet("{id}")]
        public ActionResult<UserResponse> GetById([FromRoute] Guid id)
        {
            var user = userService.GetById(id);
            if (user == null) return NotFound($"No existe un elemento con el id {id}.");
            return Ok(ToResponse(user));
        }

        [HttpPut("{id}")]
        public ActionResult<UserResponse> Update([FromRoute] Guid id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = userService.Update(id, request);
                if (user == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(ToResponse(user));
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpPatch("client/{id}/phone")]
        public ActionResult<UserResponse> UpdatePhone([FromRoute] Guid id, [FromBody] UpdatePhoneRequest request)
        {
            try
            {
                var user = userService.UpdateClientPhone(id, request.Phone);
                if (user == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(ToResponse(user));
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [HttpPatch("lawyer/{id}/phone")]
        public ActionResult<UserResponse> UpdateLawyerPhone([FromRoute] Guid id, [FromBody] UpdatePhoneRequest request)
        {
            try
            {
                var user = userService.UpdateLawyerPhone(id, request.Phone);
                if (user == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(ToResponse(user));
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [HttpPatch("client/{id}/address")]
        public ActionResult<UserResponse> UpdateAddress([FromRoute] Guid id, [FromBody] UpdateAddressRequest request)
        {
            try
            {
                var user = userService.UpdateClientAddress(id, request.Address);
                if (user == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(ToResponse(user));
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [HttpPatch("lawyer/{id}/bar-number")]
        public ActionResult<UserResponse> UpdateBarNumber([FromRoute] Guid id, [FromBody] UpdateBarNumberRequest request)
        {
            try
            {
                var user = userService.UpdateBarNumber(id, request.BarNumber);
                if (user == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(ToResponse(user));
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [HttpPatch("lawyer/{id}/specialties")]
        public ActionResult<UserResponse> UpdateSpecialties([FromRoute] Guid id, [FromBody] UpdateSpecialtiesRequest request)
        {
            try
            {
                var user = userService.UpdateSpecialties(id, request.Specialties);
                if (user == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(ToResponse(user));
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [HttpDelete("{id}")]
        public ActionResult Delete([FromRoute] Guid id)
        {
            try
            {
                if (!userService.Delete(id))
                    return NotFound($"No existe un elemento con el id {id}.");
                return NoContent();
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }
    }
}