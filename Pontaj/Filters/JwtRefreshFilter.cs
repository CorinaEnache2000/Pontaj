using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Pontaj.Models;
using Pontaj.Services.Login;

namespace Pontaj.Filters;

public class JwtRefreshFilter : IAsyncActionFilter
{
    private readonly IJwtTokenService _tokenService;
    private readonly JwtRuntimeOptions _options;

    public JwtRefreshFilter(IJwtTokenService tokenService, JwtRuntimeOptions options)
    {
        _tokenService = tokenService;
        _options = options;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();

        if (executed.Canceled || executed.Exception != null)
        {
            return;
        }

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (executed.Result is not ObjectResult objectResult || objectResult.Value is not ResponseBase response)
        {
            return;
        }

        if (!ShouldRefresh(user))
        {
            return;
        }

        var fresh = _tokenService.CreateTokenFromPrincipal(user);
        response.Token = fresh.Token;
    }

    private bool ShouldRefresh(System.Security.Claims.ClaimsPrincipal user)
    {
        var expClaim = user.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        if (string.IsNullOrEmpty(expClaim) || !long.TryParse(expClaim, out var expUnix))
        {
            return false;
        }

        var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var remaining = expUnix - nowUnix;
        if (remaining <= 0)
        {
            return false;
        }

        var thresholdSeconds = _options.TokenLifetime.TotalSeconds * _options.RefreshThresholdPercent / 100.0;
        return remaining < thresholdSeconds;
    }
}
