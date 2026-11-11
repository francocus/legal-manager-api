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
using LegalManager.Domain.Interfaces;
using LegalManager.Application.Common.Exceptions;

namespace LegalManager.Application.Auth.Services
{
    public class AuthService(
        IUserRepository usersRepository,
        ITokenService tokenService,
        IPasswordHasher passwordHasher) : IAuthService
    {
        public async Task<LoginResponse?> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            var user = await usersRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (user == null || !user.Active || !passwordHasher.Verify(request.Password, user.PasswordHash))
                return null;

            if (!user.EmailConfirmed)
                throw new ForbiddenException("Debés confirmar tu email antes de iniciar sesión.");

            return new LoginResponse(tokenService.GenerateToken(user.Id, user.Email, user.GetType().Name));
        }
    }
}