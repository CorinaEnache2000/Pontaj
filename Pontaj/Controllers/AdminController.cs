using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pontaj.Services.Login;

namespace Pontaj.Controllers
{
    [Authorize(AuthenticationSchemes = AuthSchemes.JwtCookie)]
    public class AdminController : Controller
    {
        /*private readonly ILogger<ReportsController> _logger;

        public OrganizationalUnitsController(ILogger<OrganizationalUnitsController> logger)
        {
            _logger = logger;
        }*/
        public IActionResult Index()
        {
            return View();
        }
    }
}
