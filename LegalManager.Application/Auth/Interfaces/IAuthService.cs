using LegalManager.Application.Auth.DTOs;

namespace LegalManager.Application.Auth.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse?> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default);
    }
}