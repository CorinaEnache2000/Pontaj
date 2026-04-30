using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Pontaj.Database.Pontaj;

namespace Pontaj.Services.Login;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtRuntimeOptions _options;

    public JwtTokenService(JwtRuntimeOptions options)
    {
        _options = options;
    }

    public JwtTokenResult CreateToken(AppUser user, IEnumerable<UserRole> roles, string? displayName)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.ID.ToString()),
            new(ClaimTypes.NameIdentifier, user.ID.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("DisplayName", displayName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Name));
        }

        return BuildToken(claims);
    }

    public JwtTokenResult CreateTokenFromPrincipal(ClaimsPrincipal principal)
    {
        // Re-issue with the same identity claims and a fresh exp/jti.
        // Filters out time/signature claims that JwtSecurityToken regenerates itself.
        var excluded = new HashSet<string>(StringComparer.Ordinal)
        {
            JwtRegisteredClaimNames.Exp,
            JwtRegisteredClaimNames.Iat,
            JwtRegisteredClaimNames.Nbf,
            JwtRegisteredClaimNames.Iss,
            JwtRegisteredClaimNames.Aud,
            JwtRegisteredClaimNames.Jti
        };

        var claims = principal.Claims
            .Where(c => !excluded.Contains(c.Type))
            .Select(c => new Claim(c.Type, c.Value))
            .ToList();

        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
        return BuildToken(claims);
    }

    private JwtTokenResult BuildToken(IList<Claim> claims)
    {
        var expiresAt = DateTime.UtcNow.Add(_options.TokenLifetime);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: JwtSettings.Issuer,
            audience: JwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return new JwtTokenResult(tokenString, expiresAt);
    }
}
