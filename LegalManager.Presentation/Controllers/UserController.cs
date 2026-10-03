using LegalManager.Application.DTOs;
using LegalManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegalManager.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Policies.AllRoles)]
    public class UserController(IUserService userService) : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("client")]
        public ActionResult<UserResponse> CreateClient([FromBody] CreateClientRequest request)
        {
            try
            {
                var client = userService.CreateClient(request);
                return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminsOnly)]
        [HttpPost("lawyer")]
        public ActionResult<UserResponse> CreateLawyer([FromBody] CreateLawyerRequest request)
        {
            try
            {
                var lawyer = userService.CreateLawyer(request);
                return CreatedAtAction(nameof(GetById), new { id = lawyer.Id }, lawyer);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminsOnly)]
        [HttpPost("admin")]
        public ActionResult<UserResponse> CreateAdmin([FromBody] CreateAdminRequest request)
        {
            try
            {
                var admin = userService.CreateAdmin(request);
                return CreatedAtAction(nameof(GetById), new { id = admin.Id }, admin);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminsOnly)]
        [HttpGet]
        public ActionResult<IReadOnlyList<UserResponse>> GetAll()
        {
            var users = userService.GetAll();
            return Ok(users);
        }

        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpGet("clients")]
        public ActionResult<IReadOnlyList<UserResponse>> GetClients()
            => Ok(userService.GetClients());

        [HttpGet("lawyers")]
        public ActionResult<IReadOnlyList<UserResponse>> GetLawyers()
            => Ok(userService.GetLawyers());

        [Authorize(Policy = Policies.AdminsOnly)]
        [HttpGet("admins")]
        public ActionResult<IReadOnlyList<UserResponse>> GetAdmins()
            => Ok(userService.GetAdmins());

        [HttpGet("{id}")]
        public ActionResult<UserResponse> GetById([FromRoute] Guid id)
        {
            var user = userService.GetById(id);
            if (user == null) return NotFound($"No existe un elemento con el id {id}.");
            return Ok(user);
        }

        [Authorize(Policy = Policies.AdminsOnly)]
        [HttpPut("{id}")]
        public ActionResult<UserResponse> Update([FromRoute] Guid id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = userService.Update(id, request);
                if (user == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(user);
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
                return Ok(user);
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
                return Ok(user);
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
                return Ok(user);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminsOnly)]
        [HttpPatch("lawyer/{id}/bar-number")]
        public ActionResult<UserResponse> UpdateBarNumber([FromRoute] Guid id, [FromBody] UpdateBarNumberRequest request)
        {
            try
            {
                var user = userService.UpdateBarNumber(id, request.BarNumber);
                if (user == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(user);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminsOnly)]
        [HttpPatch("lawyer/{id}/specialties")]
        public ActionResult<UserResponse> UpdateSpecialties([FromRoute] Guid id, [FromBody] UpdateSpecialtiesRequest request)
        {
            try
            {
                var user = userService.UpdateSpecialties(id, request.Specialties);
                if (user == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(user);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminsOnly)]
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