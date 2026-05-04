using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pontaj.Models;
using Pontaj.Services.Login;

namespace Pontaj.Controllers
{
    [Authorize(AuthenticationSchemes = AuthSchemes.JwtCookie)]
    public class ReportsController : Controller
    {
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(ILogger<ReportsController> logger)
        {
            _logger = logger;
        }
        public IActionResult Index()
        {
            return View("Reports");
        }
    }
}
