using LegalManager.Application.DTOs;
using LegalManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegalManager.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var login = await authService.AuthenticateAsync(request, cancellationToken);

            if (login == null)
                return Unauthorized("Email o contraseña incorrectos.");

            return Ok(login);
        }
    }
}