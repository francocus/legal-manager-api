using LegalManager.Application.DTOs;

namespace LegalManager.Application.Interfaces
{
    public interface IAuthService
    {
        LoginResponse? Authenticate(LoginRequest request);
    }
}