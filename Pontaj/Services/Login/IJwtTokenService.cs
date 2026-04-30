using System.Security.Claims;
using Pontaj.Database.Pontaj;

namespace Pontaj.Services.Login;

public interface IJwtTokenService
{
    JwtTokenResult CreateToken(AppUser user, IEnumerable<UserRole> roles, string? displayName);
    JwtTokenResult CreateTokenFromPrincipal(ClaimsPrincipal principal);
}

public record JwtTokenResult(string Token, DateTime ExpiresAtUtc);
