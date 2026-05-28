using System.Runtime.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pontaj.Models;
using Pontaj.Services.Login;
using Pontaj.Services.Logs;

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
    private readonly IAppLogger _logger;

    public AccountController(
        IActiveDirectoryService adService,
        IRoleService roleService,
        IUserService userService,
        IJwtTokenService tokenService,
        IAppLogger logger)
    {
        _adService = adService;
        _roleService = roleService;
        _userService = userService;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(ResponseBase.Error("Utilizatorul și parola sunt obligatorii."));
            }

            if (!_adService.Authenticate(request.Username, request.Password))
            {
                await TryLogAsync("Login_Failed", $"Tentativă de autentificare eșuată pentru user: {request.Username}");
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
                await TryLogAsync("Login_Forbidden", $"Utilizatorul {request.Username} nu are grupuri de AD mapate pe roluri.");
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    ResponseBase.Error("Nu aveți drept de acces la această aplicație."));
            }

            var dbUser = await _userService.GetOrCreateUserAsync(request.Username, ct);

            var isAdmin = roles.Any(r => r.Name == "ADMINISTRATOR");
            if (!dbUser.EmployeeId.HasValue && !isAdmin)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    ResponseBase.Error(
                        "Contul dumneavoastră nu a putut fi asociat. Vă rugăm contactați echipa IT.",
                        new { code = "UNLINKED" }));
            }

            await _userService.SyncUserRolesAsync(dbUser.Id, roles, ct);

            var token = _tokenService.CreateToken(dbUser, roles, adUser.DisplayName);

            await TryLogAsync("Login_Success", $"Utilizatorul {request.Username} s-a logat.", null, request.Username);

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
        catch (Exception ex)
        {
            await TryLogAsync("Login_Error", $"Eroare critică la login pentru {request.Username}", ex);
            return StatusCode(500, ResponseBase.Error("Eroare internă de server."));
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await TryLogAsync("Logout", $"Utilizatorul {User.Identity?.Name} s-a delogat.");
        return Ok(new { success = true });
    }

    private async Task TryLogAsync(string action, string message, Exception? ex = null, string? username = null)
    {
        try
        {
            await _logger.LogAsync(action, message, ex, username);
        }
        catch
        {
        }
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
