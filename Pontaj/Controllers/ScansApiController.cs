using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pontaj.Models;
using Pontaj.Models.Home;
using Pontaj.Services.Login;
using Pontaj.Services.Logs;
using Pontaj.Services.Scan;

namespace Pontaj.Controllers;

[ApiController]
[Route("api/scans")]
[Authorize(AuthenticationSchemes = AuthSchemes.JwtHeader)]
public class ScansApiController : ControllerBase
{
    private readonly IScanService _scanService;
    private readonly IAppLogger _logger;

    public ScansApiController(IScanService scanService, IAppLogger logger)
    {
        _scanService = scanService;
        _logger = logger;
    }

    [HttpPost("list")]
    public async Task<IActionResult> List([FromBody] ScanPageRequest request, CancellationToken ct)
    {
        if (request == null)
        {
            return BadRequest(ResponseBase.Error("Cerere invalidă."));
        }

        try
        {
            var scope = await _scanService.ResolveScopeAsync(User, ct);

            if (!scope.IsAdmin && !scope.SelfEmployeeId.HasValue)
            {
                return Ok(ResponseBase.Success(new
                {
                    items = Array.Empty<object>(),
                    total = 0,
                    page = request.Page,
                    pageSize = request.PageSize,
                    notLinked = true
                }));
            }

            var (items, total) = await _scanService.GetPageAsync(request, scope, ct);
            var payload = items.Select(i => new
            {
                id = i.Id,
                employeeId = i.EmployeeId,
                employeeName = $"{i.EmployeeLastName} {i.EmployeeFirstName}".Trim(),
                mark = i.Mark,
                badge = i.Badge,
                inOut = i.InOut,
                moment = i.InsertedMoment.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                workStationId = i.WorkStationId,
                workStation = i.WorkStationName,
                organizationalUnitId = i.OrganizationalUnitId,
                organizationalUnit = i.OrganizationalUnitName,
                ip = i.Ip
            }).ToList();

            return Ok(ResponseBase.Success(new
            {
                items = payload,
                total,
                page = request.Page,
                pageSize = request.PageSize
            }));
        }
        catch (Exception ex)
        {
            await TryLogAsync("Punch_List_Error", "Eroare la încărcarea pontajelor.", ex);
            return StatusCode(500, ResponseBase.Error("Eroare la încărcarea pontajelor."));
        }
    }

    [HttpPost("create")]
    [Authorize(AuthenticationSchemes = AuthSchemes.JwtHeader, Roles = "ADMINISTRATOR,DIRECTOR")]
    public async Task<IActionResult> Create([FromBody] ScanCreateRequest request, CancellationToken ct)
    {
        if (request == null)
        {
            return BadRequest(ResponseBase.Error("Cerere invalidă."));
        }

        try
        {
            var scope = await _scanService.ResolveScopeAsync(User, ct);
            var actor = User.Identity?.Name ?? "Anonymous";
            var (error, created) = await _scanService.CreateAsync(request, scope, actor, ct);
            if (error != null)
            {
                return BadRequest(ResponseBase.Error(error));
            }
            return Ok(ResponseBase.Success(new { id = created!.Id }));
        }
        catch (Exception ex)
        {
            await TryLogAsync("Punch_Create_Error", "Eroare la crearea pontajului.", ex);
            return StatusCode(500, ResponseBase.Error("Eroare la crearea pontajului."));
        }
    }

    [HttpPost("update")]
    [Authorize(AuthenticationSchemes = AuthSchemes.JwtHeader, Roles = "ADMINISTRATOR,DIRECTOR")]
    public async Task<IActionResult> Update([FromBody] ScanUpdateRequest request, CancellationToken ct)
    {
        if (request == null)
        {
            return BadRequest(ResponseBase.Error("Cerere invalidă."));
        }

        try
        {
            var scope = await _scanService.ResolveScopeAsync(User, ct);
            var actor = User.Identity?.Name ?? "Anonymous";
            var (error, _) = await _scanService.UpdateAsync(request, scope, actor, ct);
            if (error != null)
            {
                return BadRequest(ResponseBase.Error(error));
            }
            return Ok(ResponseBase.Success());
        }
        catch (Exception ex)
        {
            await TryLogAsync("Punch_Update_Error", "Eroare la actualizarea pontajului.", ex);
            return StatusCode(500, ResponseBase.Error("Eroare la actualizarea pontajului."));
        }
    }

    [HttpPost("delete")]
    [Authorize(AuthenticationSchemes = AuthSchemes.JwtHeader, Roles = "ADMINISTRATOR,DIRECTOR")]
    public async Task<IActionResult> Delete([FromBody] ScanDeleteRequest request, CancellationToken ct)
    {
        if (request == null)
        {
            return BadRequest(ResponseBase.Error("Cerere invalidă."));
        }

        try
        {
            var scope = await _scanService.ResolveScopeAsync(User, ct);
            var actor = User.Identity?.Name ?? "Anonymous";
            var error = await _scanService.DeleteAsync(request.Id, scope, actor, ct);
            if (error != null)
            {
                return BadRequest(ResponseBase.Error(error));
            }
            return Ok(ResponseBase.Success());
        }
        catch (Exception ex)
        {
            await TryLogAsync("Punch_Delete_Error", "Eroare la ștergerea pontajului.", ex);
            return StatusCode(500, ResponseBase.Error("Eroare la ștergerea pontajului."));
        }
    }

    private async Task TryLogAsync(string action, string message, Exception? ex = null)
    {
        try
        {
            await _logger.LogAsync(action, message, ex);
        }
        catch
        {
        }
    }
}
