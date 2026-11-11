using LegalManager.Presentation.Auth;
using LegalManager.Application.Auth.DTOs;
using LegalManager.Application.Users.DTOs;
using LegalManager.Application.Cases.DTOs;
using LegalManager.Application.Appointments.DTOs;
using LegalManager.Application.Documents.DTOs;
using LegalManager.Application.Common;
using LegalManager.Application.Auth.Interfaces;
using LegalManager.Application.Users.Interfaces;
using LegalManager.Application.Cases.Interfaces;
using LegalManager.Application.Appointments.Interfaces;
using LegalManager.Application.Documents.Interfaces;
using LegalManager.Application.Common.Interfaces;
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
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
        {
            try
            {
                if (page is null && pageSize is null)
                    return Ok(await userService.GetAllAsync(cancellationToken));

                return Ok(await userService.GetPagedAsync(page, pageSize, cancellationToken));
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminOrLawyer)]
        [HttpGet("clients")]
        public async Task<IActionResult> GetClients([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
        {
            try
            {
                if (page is null && pageSize is null)
                    return Ok(await userService.GetClientsAsync(cancellationToken));

                return Ok(await userService.GetClientsPagedAsync(page, pageSize, cancellationToken));
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("lawyers")]
        public async Task<IActionResult> GetLawyers([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
        {
            try
            {
                if (page is null && pageSize is null)
                    return Ok(await userService.GetLawyersAsync(cancellationToken));

                return Ok(await userService.GetLawyersPagedAsync(page, pageSize, cancellationToken));
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [Authorize(Policy = Policies.AdminsOnly)]
        [HttpGet("admins")]
        public async Task<IActionResult> GetAdmins([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
        {
            try
            {
                if (page is null && pageSize is null)
                    return Ok(await userService.GetAdminsAsync(cancellationToken));

                return Ok(await userService.GetAdminsPagedAsync(page, pageSize, cancellationToken));
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

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
        [HttpPatch("{id}/confirm-email")]
        public async Task<ActionResult<UserResponse>> ConfirmEmailByAdmin([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var user = await userService.ConfirmEmailByAdminAsync(id, cancellationToken);
            if (user == null) return NotFound($"No existe un elemento con el id {id}.");
            return Ok(user);
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

        [Authorize(Policy = Policies.AdminsOnly)]
        [HttpPatch("{id}/role")]
        public async Task<ActionResult<UserResponse>> ChangeRole([FromRoute] Guid id, [FromBody] RoleChangeRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await userService.ChangeRoleAsync(id, request, cancellationToken);
                if (user == null) return NotFound($"No existe un elemento con el id {id}.");
                return Ok(user);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }
    }
}