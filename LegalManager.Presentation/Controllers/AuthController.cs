using LegalManager.Presentation.Auth;
using LegalManager.Application;
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
using LegalManager.Application.Auth.Services;
using LegalManager.Application.Users.Services;
using LegalManager.Application.Cases.Services;
using LegalManager.Application.Appointments.Services;
using LegalManager.Application.Documents.Services;
using LegalManager.Domain;
using LegalManager.Domain.Enums;
using LegalManager.Domain.ValueObjects;
using LegalManager.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalManager.Application.Common.Exceptions;

namespace LegalManager.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController(IAuthService authService, IUserService userService, AccountConfirmationService confirmationService) : ControllerBase
    {
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var login = await authService.AuthenticateAsync(request, cancellationToken);
            if (login == null)
                return Unauthorized("Email o contraseña incorrectos.");

            return Ok(login);
        }

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

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await confirmationService.ConfirmByTokenAsync(request.Token, cancellationToken);
                return Ok(new { message = "Tu email quedó confirmado. Ya podés iniciar sesión." });
            }
            catch (AccountLinkException ex) { return BadRequest(ex.Message); }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await confirmationService.ForgotPasswordAsync(request.Email, cancellationToken);
                return Ok(new { message = "Si el email está registrado, te enviamos un correo con instrucciones." });
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await confirmationService.ResetPasswordAsync(request.Token, request.NewPassword, cancellationToken);
                return Ok(new { message = "Tu contraseña quedó restablecida. Ya podés iniciar sesión." });
            }
            catch (AccountLinkException ex) { return BadRequest(ex.Message); }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await confirmationService.ResendConfirmationAsync(request.Email, cancellationToken);
                return Ok(new { message = "Si tu cuenta sigue pendiente, te enviamos uno nuevo." });
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }
    }
}