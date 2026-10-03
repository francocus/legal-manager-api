using System.Security.Claims;
using LegalManager.Application.Interfaces;
using LegalManager.Domain.Entities;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LegalManager.Presentation.Security
{
    public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
    {
        private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

        public Guid Id
        {
            get
            {
                var value = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                return Guid.TryParse(value, out var id) ? id : Guid.Empty;
            }
        }

        public string Role => Principal?.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        public bool IsAdmin => Is(nameof(Admin));

        public bool IsLawyer => Is(nameof(Lawyer));

        public bool IsClient => Is(nameof(Client));

        private bool Is(string role) =>
            string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);
    }
}