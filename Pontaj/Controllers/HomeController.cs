using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pontaj.Models;
using Pontaj.Services.Login;
using Pontaj.Services.Scan;

namespace Pontaj.Controllers;

[Authorize(AuthenticationSchemes = AuthSchemes.JwtCookie)]
public class HomeController : Controller
{
    private readonly IScanService _scanService;

    public HomeController(IScanService scanService)
    {
        _scanService = scanService;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var scope = await _scanService.ResolveScopeAsync(User, ct);
        var vm = await _scanService.BuildIndexViewModelAsync(scope, ct);
        return View(vm);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
