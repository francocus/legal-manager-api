using LegalManager.Application.DTOs;
using LegalManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegalManager.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController(IAuthService authService, IUserService userService) : ControllerBase
    {
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var login = await authService.AuthenticateAsync(request, cancellationToken);

            if (login == null)
                return Unauthorized("Email o contraseña incorrectos.");

            return Ok(login);
        }

        // Public registration: every self-service account is a Client. No role is
        // accepted from the request; privileged roles remain created through the
        // admin user flow.
        [HttpPost("register")]
        public async Task<ActionResult<UserResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var client = await userService.CreateClientAsync(
                    new CreateClientRequest(
                        request.FirstName,
                        request.LastName,
                        request.Dni,
                        request.Email,
                        request.Password,
                        Phone: null,
                        Address: null),
                    cancellationToken);

                return CreatedAtAction(nameof(UserController.GetById), "User", new { id = client.Id }, client);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }
    }
}