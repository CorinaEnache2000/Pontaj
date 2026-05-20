using System.Runtime.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pontaj.Database.Pontaj;
using Pontaj.Models;
using Pontaj.Models.Scan;
using Pontaj.Repositories;
using Pontaj.Services.Logs;

namespace Pontaj.Controllers;

[ApiController]
[Route("api/[controller]")]
[SupportedOSPlatform("windows")]
[AllowAnonymous]
[EnableRateLimiting("scan")]
public class ScanController : ControllerBase
{
    private readonly IEmployeeRepository _employees;
    private readonly IWorkStationRepository _workStations;
    private readonly IPunchRepository _punches;
    private readonly IAppLogger _logger;

    public ScanController(
        IEmployeeRepository employees,
        IWorkStationRepository workStations,
        IPunchRepository punches,
        IAppLogger logger)
    {
        _employees = employees;
        _workStations = workStations;
        _punches = punches;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ScanRequest request, CancellationToken ct)
    {
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var badge = request?.Badge?.Trim();
        var hostname = request?.Hostname?.Trim();

        try
        {
            if (string.IsNullOrWhiteSpace(badge))
            {
                return BadRequest(ResponseBase.Error("Cardul este obligatoriu."));
            }

            var employee = await _employees.GetActiveByBadgeAsync(badge, ct);
            if (employee == null)
            {
                await TryLogAsync(
                    "Scan_UnknownBadge",
                    $"Card necunoscut '{badge}' de la IP {remoteIp ?? "(necunoscut)"}, host {hostname ?? "(necunoscut)"}.",
                    null,
                    $"scan@{remoteIp ?? "unknown"}");
                return NotFound(ResponseBase.Error("Card necunoscut. Acces refuzat."));
            }

            WorkStations? workStation = null;
            int? employeeOuId = await _employees.GetPrimaryActiveOrganizationalUnitIdAsync(employee.Id, ct);

            if (!string.IsNullOrWhiteSpace(hostname) && employeeOuId.HasValue)
            {
                workStation = await _workStations.GetOrCreateByHostnameAsync(
                    hostname, remoteIp, employeeOuId.Value, ct);
            }
            else if (!string.IsNullOrWhiteSpace(hostname))
            {
                workStation = await _workStations.GetActiveByHostnameAsync(hostname, ct);
            }

            if (workStation == null && !string.IsNullOrEmpty(remoteIp))
            {
                workStation = await _workStations.GetActiveByIpAsync(remoteIp, ct);
            }

            int? organizationalUnitId = workStation?.OrganizationalUnitId ?? employeeOuId;

            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);

            var punch = new Punches
            {
                WorkStationId = workStation?.Id,
                OrganizationalUnitId = organizationalUnitId,
                EmployeeId = employee.Id,
                Badge = badge,
                InsertedDate = today,
                InsertedTime = TimeOnly.FromDateTime(now),
                InsertedMoment = now,
                Ip = remoteIp
            };

            await _punches.InsertWithDirectionInferenceAsync(punch, ct);

            var data = new ScanResponseData(
                DisplayName: $"{employee.LastName} {employee.FirstName}",
                Direction: punch.InOut ? "IN" : "OUT",
                Moment: now);

            return Ok(ResponseBase.Success(data));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await TryLogAsync(
                "Scan_Error",
                $"Eroare la procesarea scanării pentru cardul '{badge ?? "(gol)"}' de la IP {remoteIp ?? "(necunoscut)"}.",
                ex,
                $"scan@{remoteIp ?? "unknown"}");
            return StatusCode(500, ResponseBase.Error("Eroare internă de server."));
        }
    }

    private async Task TryLogAsync(string action, string message, Exception? ex, string username)
    {
        try
        {
            await _logger.LogAsync(action, message, ex, username);
        }
        catch
        {
        }
    }
}
