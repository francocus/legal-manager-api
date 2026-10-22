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
        public async Task<ActionResult<UserResponse>> CreateClient([FromBody] CreateClientRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var client = await userService.CreateClientAsync(request, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminsOnly)]
        [HttpPost("lawyer")]
        public async Task<ActionResult<UserResponse>> CreateLawyer([FromBody] CreateLawyerRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var lawyer = await userService.CreateLawyerAsync(request, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = lawyer.Id }, lawyer);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminsOnly)]
        [HttpPost("admin")]
        public async Task<ActionResult<UserResponse>> CreateAdmin([FromBody] CreateAdminRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var admin = await userService.CreateAdminAsync(request, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = admin.Id }, admin);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminsOnly)]
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetAll(CancellationToken cancellationToken)
        {
            var users = await userService.GetAllAsync(cancellationToken);
            return Ok(users);
        }

        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpGet("clients")]
        public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetClients(CancellationToken cancellationToken)
            => Ok(await userService.GetClientsAsync(cancellationToken));

        [HttpGet("lawyers")]
        public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetLawyers(CancellationToken cancellationToken)
            => Ok(await userService.GetLawyersAsync(cancellationToken));

        [Authorize(Policy = Policies.AdminsOnly)]
        [HttpGet("admins")]
        public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetAdmins(CancellationToken cancellationToken)
            => Ok(await userService.GetAdminsAsync(cancellationToken));

        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponse>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var user = await userService.GetByIdAsync(id, cancellationToken);
            if (user == null) return NotFound($"No existe un elemento con el id {id}.");
            return Ok(user);
        }

        [Authorize(Policy = Policies.AdminsOnly)]
        [HttpPut("{id}")]
        public async Task<ActionResult<UserResponse>> Update([FromRoute] Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await userService.UpdateAsync(id, request, cancellationToken);
                if (user == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(user);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpPatch("client/{id}/phone")]
        public async Task<ActionResult<UserResponse>> UpdatePhone([FromRoute] Guid id, [FromBody] UpdatePhoneRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await userService.UpdateClientPhoneAsync(id, request.Phone, cancellationToken);
                if (user == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(user);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [HttpPatch("lawyer/{id}/phone")]
        public async Task<ActionResult<UserResponse>> UpdateLawyerPhone([FromRoute] Guid id, [FromBody] UpdatePhoneRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await userService.UpdateLawyerPhoneAsync(id, request.Phone, cancellationToken);
                if (user == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(user);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [HttpPatch("client/{id}/address")]
        public async Task<ActionResult<UserResponse>> UpdateAddress([FromRoute] Guid id, [FromBody] UpdateAddressRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await userService.UpdateClientAddressAsync(id, request.Address, cancellationToken);
                if (user == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(user);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminsOnly)]
        [HttpPatch("lawyer/{id}/bar-number")]
        public async Task<ActionResult<UserResponse>> UpdateBarNumber([FromRoute] Guid id, [FromBody] UpdateBarNumberRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await userService.UpdateBarNumberAsync(id, request.BarNumber, cancellationToken);
                if (user == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(user);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminsOnly)]
        [HttpPatch("lawyer/{id}/specialties")]
        public async Task<ActionResult<UserResponse>> UpdateSpecialties([FromRoute] Guid id, [FromBody] UpdateSpecialtiesRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await userService.UpdateSpecialtiesAsync(id, request.Specialties, cancellationToken);
                if (user == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(user);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminsOnly)]
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            try
            {
                if (!await userService.DeleteAsync(id, cancellationToken))
                    return NotFound($"No existe un elemento con el id {id}.");
                return NoContent();
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }
    }
}