using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pontaj.Models;
using Pontaj.Services.Admin;
using Pontaj.Services.Login;
using Pontaj.Services.Logs;

namespace Pontaj.Controllers
{
    [Authorize(AuthenticationSchemes = AuthSchemes.JwtCookie)]
    public class AdminController : Controller
    {
        private readonly IEmployeeAdminService _employeeAdminService;
        private readonly IOrganizationalUnitAdminService _organizationalUnitAdminService;
        private readonly IUserAdminService _userAdminService;
        private readonly IAppLogger _logger;

        public AdminController(
            IEmployeeAdminService employeeAdminService,
            IOrganizationalUnitAdminService organizationalUnitAdminService,
            IUserAdminService userAdminService,
            IAppLogger logger)
        {
            _employeeAdminService = employeeAdminService;
            _organizationalUnitAdminService = organizationalUnitAdminService;
            _userAdminService = userAdminService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Organizational-units admin page: the full parent/child tree, with a
        // client-side name filter.
        public async Task<IActionResult> OrganizationalUnits(CancellationToken ct)
        {
            var model = await _organizationalUnitAdminService.GetTreeAsync(ct);
            return View("OrganizationalUnits/OrganizationalUnits", model);
        }

        public async Task<IActionResult> OrganizationalUnitGeneralInfo(int id, CancellationToken ct)
        {
            var detail = await _organizationalUnitAdminService.GetDetailAsync(id, ct);
            if (detail == null)
            {
                return NotFound();
            }

            return PartialView("OrganizationalUnits/_OrganizationalUnitGeneralInfo", detail);
        }

        public async Task<IActionResult> OrganizationalUnitWorkStations(int id, CancellationToken ct)
        {
            var workStations = await _organizationalUnitAdminService.GetWorkStationsAsync(id, ct);
            return PartialView("OrganizationalUnits/_OrganizationalUnitWorkStations", workStations);
        }

        // Employees admin page: left-side list rendered server-side, right-side
        // detail loaded on demand via EmployeeGeneralInfo (HTML-over-XHR).
        public async Task<IActionResult> Employees(CancellationToken ct)
        {
            var model = await _employeeAdminService.GetEmployeesViewModelAsync(ct);
            return View("Employees/Employees", model);
        }

        public async Task<IActionResult> EmployeeGeneralInfo(int id, CancellationToken ct)
        {
            var detail = await _employeeAdminService.GetEmployeeDetailAsync(id, ct);
            if (detail == null)
            {
                return NotFound();
            }

            return PartialView("Employees/_EmployeeGeneralInfo", detail);
        }

        // Users admin page: left-side list rendered server-side, right-side
        // tabbed detail (General / Roluri) loaded on demand via XHR.
        public async Task<IActionResult> Users(CancellationToken ct)
        {
            var model = await _userAdminService.GetUsersViewModelAsync(ct);
            return View("Users/Users", model);
        }

        public async Task<IActionResult> UserGeneralInfo(int id, CancellationToken ct)
        {
            var detail = await _userAdminService.GetDetailAsync(id, ct);
            if (detail == null)
            {
                return NotFound();
            }

            return PartialView("Users/_UserGeneralInfo", detail);
        }

        public async Task<IActionResult> UserRoles(int id, CancellationToken ct)
        {
            var roles = await _userAdminService.GetRolesAsync(id, ct);
            return PartialView("Users/_UserRoles", roles);
        }

        [HttpPost]
        public async Task<IActionResult> SyncEmployees(CancellationToken ct)
        {
            try
            {
                var synced = await _employeeAdminService.SyncEmployeesAsync(ct);
                await TryLogAsync(
                    "SyncEmployees",
                    $"Sincronizare angajați finalizată ({synced} înregistrări procesate).");
                return Ok(ResponseBase.Success(new { synced }));
            }
            catch (Exception ex)
            {
                await TryLogAsync("SyncEmployees_Error", "Eroare la sincronizarea angajaților.", ex);
                return StatusCode(500, ResponseBase.Error("Eroare la sincronizarea angajaților."));
            }
        }

        // Swallow secondary failures so a logger glitch can't turn a clean
        // response into a 500. Missing one log row is preferable.
        private async Task TryLogAsync(string action, string message, Exception? ex = null)
        {
            try
            {
                await _logger.LogAsync(action, message, ex);
            }
            catch
            {
                // Intentionally swallowed.
            }
        }
    }
}
