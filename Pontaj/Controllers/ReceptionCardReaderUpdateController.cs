using System;
using System.IO;
using System.Runtime.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Pontaj.Controllers;

// Update manifest for the ReceptionCardReaderService self-updater. That
// reception-desk client polls this route over HTTPS with NO bearer token
// (the device path is tokenless, exactly like ScanController), so the
// endpoint must be [AllowAnonymous] — otherwise the app-wide
// FallbackPolicy = RequireAuthenticatedUser answers 401 and the client
// can never read the manifest.
//
// The body is the raw shape the client deserializes — { shouldUpdate,
// archiveURL } — NOT the ResponseBase envelope. The client does no version
// math of its own: it trusts shouldUpdate as the sole loop-breaker. So this
// endpoint compares the reported currentClientVersion against the published
// TargetVersion and only returns shouldUpdate = true when the client is
// strictly behind — an updated client (which reports TargetVersion) is then
// told "no update" and the loop ends. archiveURL points at the Download
// action below, which streams a binaries-only .zip (exe at the root).
[ApiController]
[Route("CheckForReceptionCardReaderUpdates")]
[AllowAnonymous]
[SupportedOSPlatform("windows")]
public class ReceptionCardReaderUpdateController : ControllerBase
{
    // The build currently published for the reception desks. Bump this only
    // when a newer build's archive is actually being served (see Download);
    // until then it must match the deployed version so no client is told to
    // update. ("1.1" was used transiently to exercise the updater end-to-end.)
    private const string TargetVersion = "1.0";

    // Physical file served as the update archive. Swap this file under
    // wwwroot/updates to switch between a good and a deliberately broken
    // payload while testing the updater's failure paths.
    private const string ArchiveFileName = "rcr-update.zip";

    private readonly IWebHostEnvironment _environment;

    public ReceptionCardReaderUpdateController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet]
    public IActionResult Get([FromQuery] string? currentClientVersion)
    {
        var shouldUpdate = IsBehind(currentClientVersion, TargetVersion);
        string? archiveUrl = shouldUpdate
            ? $"{Request.Scheme}://{Request.Host}/CheckForReceptionCardReaderUpdates/download"
            : null;

        return Ok(new { shouldUpdate, archiveURL = archiveUrl });
    }

    [HttpGet("download")]
    public IActionResult Download()
    {
        var path = ArchivePath();
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        return PhysicalFile(path, "application/zip", ArchiveFileName);
    }

    private string ArchivePath()
    {
        var root = _environment.WebRootPath
            ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        return Path.Combine(root, "updates", ArchiveFileName);
    }

    private static bool IsBehind(string? clientVersion, string targetVersion)
    {
        return Version.TryParse(clientVersion, out var client)
            && Version.TryParse(targetVersion, out var target)
            && client < target;
    }
}
