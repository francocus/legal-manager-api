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
        public LoginResponse? Authenticate(LoginRequest request)
        {
            var user = usersRepository.GetByEmail(request.Email);

            if (user == null || !user.Active || !passwordHasher.Verify(request.Password, user.PasswordHash))
                return null;

            return new LoginResponse(tokenService.GenerateToken(user.Id, user.Email, user.GetType().Name));
        }
    }
}