using LegalManager.Application.DTOs;
using LegalManager.Application.Interfaces;
using LegalManager.Domain.Interfaces;

namespace LegalManager.Application.Services
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

            return new LoginResponse(tokenService.GenerateToken(user.Id, user.Email, user.GetType().Name));
        }
    }
}