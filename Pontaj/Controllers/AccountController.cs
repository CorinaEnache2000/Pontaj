using System.Runtime.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pontaj.Models;
using Pontaj.Services.Login;

namespace Pontaj.Controllers;

[ApiController]
[Route("api/[controller]")]
[SupportedOSPlatform("windows")]
[Authorize(AuthenticationSchemes = AuthSchemes.JwtHeader)]
public class AccountController : ControllerBase
{
    private readonly IActiveDirectoryService _adService;
    private readonly IRoleService _roleService;
    private readonly IUserService _userService;
    private readonly IJwtTokenService _tokenService;

    public AccountController(
        IActiveDirectoryService adService,
        IRoleService roleService,
        IUserService userService,
        IJwtTokenService tokenService)
    {
        _adService = adService;
        _roleService = roleService;
        _userService = userService;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(ResponseBase.Error("Utilizatorul și parola sunt obligatorii."));
        }

        if (!_adService.Authenticate(request.Username, request.Password))
        {
            return Unauthorized(ResponseBase.Error("Utilizator sau parolă incorectă."));
        }

        var adUser = _adService.GetUserInfo(request.Username);
        if (adUser == null)
        {
            return Unauthorized(ResponseBase.Error("Utilizatorul nu a fost găsit în Active Directory."));
        }

        var adGroups = _adService.GetUserGroups(request.Username);
        var roles = await _roleService.GetRolesFromADGroupsAsync(adGroups, ct);

        if (roles.Count == 0)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                ResponseBase.Error("Nu aveți drept de acces la această aplicație."));
        }

        var dbUser = await _userService.GetOrCreateUserAsync(request.Username, ct);
        await _userService.SyncUserRolesAsync(dbUser.ID, roles, ct);

        var token = _tokenService.CreateToken(dbUser, roles, adUser.DisplayName);

        var response = ResponseBase.Success(new
        {
            username = dbUser.Username,
            displayName = adUser.DisplayName,
            roles = roles.Select(r => r.Name).ToArray(),
            expiresAtUtc = token.ExpiresAtUtc
        });
        response.Token = token.Token;

        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("sessionToken", new CookieOptions
        {
            Path = "/", 
            HttpOnly = true,
            Secure = true
        });

        return Ok(new { success = true });
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        var data = new
        {
            username = User.Identity?.Name,
            displayName = User.FindFirst("DisplayName")?.Value,
            roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToArray()
        };

        return Ok(ResponseBase.Success(data));
    }
}

public record LoginRequest(string Username, string Password);
