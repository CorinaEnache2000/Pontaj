using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pontaj.Models;
using Pontaj.Models.Admin;
using Pontaj.Models.Admin.WorkStations;
using Pontaj.Services.Admin;
using Pontaj.Services.Login;
using Pontaj.Services.Logs;

namespace Pontaj.Controllers
{
    [Authorize(AuthenticationSchemes = AuthSchemes.JwtCookie, Roles = "ADMINISTRATOR")]
    public class AdminController : Controller
    {
        private readonly IEmployeeAdminService _employeeAdminService;
        private readonly IOrganizationalUnitAdminService _organizationalUnitAdminService;
        private readonly IUserAdminService _userAdminService;
        private readonly IWorkStationAdminService _workStationAdminService;
        private readonly IAppLogger _logger;

        public AdminController(
            IEmployeeAdminService employeeAdminService,
            IOrganizationalUnitAdminService organizationalUnitAdminService,
            IUserAdminService userAdminService,
            IWorkStationAdminService workStationAdminService,
            IAppLogger logger)
        {
            _employeeAdminService = employeeAdminService;
            _organizationalUnitAdminService = organizationalUnitAdminService;
            _userAdminService = userAdminService;
            _workStationAdminService = workStationAdminService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

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

        public async Task<IActionResult> WorkStations(CancellationToken ct)
        {
            var model = await _workStationAdminService.GetViewModelAsync(ct);
            return View("WorkStations/WorkStations", model);
        }

        public async Task<IActionResult> WorkStationGeneralInfo(int id, CancellationToken ct)
        {
            var detail = await _workStationAdminService.GetDetailAsync(id, ct);
            if (detail == null)
            {
                return NotFound();
            }

            return PartialView("WorkStations/_WorkStationGeneralInfo", detail);
        }

        [HttpPost]
        public async Task<IActionResult> CreateWorkStation(
            [FromBody] CreateWorkStationRequest request,
            CancellationToken ct)
        {
            if (request == null)
            {
                return BadRequest(ResponseBase.Error("Cerere invalidă."));
            }

            try
            {
                var (validationError, createdId) = await _workStationAdminService.CreateAsync(request, ct);
                if (validationError != null)
                {
                    return BadRequest(ResponseBase.Error(validationError));
                }

                await TryLogAsync(
                    "CreateWorkStation",
                    $"Stație de lucru creată (Id={createdId}, nume='{request.Name?.Trim()}').");

                return Ok(ResponseBase.Success(new { id = createdId }));
            }
            catch (Exception ex)
            {
                await TryLogAsync("CreateWorkStation_Error", "Eroare la crearea stației de lucru.", ex);
                return StatusCode(500, ResponseBase.Error("Eroare la crearea stației de lucru."));
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateWorkStation(
            [FromBody] UpdateWorkStationRequest request,
            CancellationToken ct)
        {
            if (request == null)
            {
                return BadRequest(ResponseBase.Error("Cerere invalidă."));
            }

            try
            {
                var validationError = await _workStationAdminService.UpdateAsync(request, ct);
                if (validationError != null)
                {
                    return BadRequest(ResponseBase.Error(validationError));
                }

                await TryLogAsync(
                    "UpdateWorkStation",
                    $"Stație de lucru actualizată (Id={request.Id}).");

                return Ok(ResponseBase.Success());
            }
            catch (Exception ex)
            {
                await TryLogAsync("UpdateWorkStation_Error", "Eroare la actualizarea stației de lucru.", ex);
                return StatusCode(500, ResponseBase.Error("Eroare la actualizarea stației de lucru."));
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetWorkStationActive(
            [FromBody] SetWorkStationActiveRequest request,
            CancellationToken ct)
        {
            if (request == null)
            {
                return BadRequest(ResponseBase.Error("Cerere invalidă."));
            }

            try
            {
                var validationError = await _workStationAdminService.SetActiveAsync(request, ct);
                if (validationError != null)
                {
                    return BadRequest(ResponseBase.Error(validationError));
                }

                await TryLogAsync(
                    "SetWorkStationActive",
                    $"Stație de lucru {(request.Active ? "activată" : "dezactivată")} (Id={request.Id}).");

                return Ok(ResponseBase.Success());
            }
            catch (Exception ex)
            {
                await TryLogAsync(
                    "SetWorkStationActive_Error",
                    "Eroare la modificarea stării stației de lucru.",
                    ex);
                return StatusCode(500, ResponseBase.Error("Eroare la modificarea stării stației de lucru."));
            }
        }

        [HttpPost]
        public Task<IActionResult> SetEmployeeActive([FromBody] SetActiveRequest request, CancellationToken ct) =>
            HandleSetActiveAsync(
                request,
                (id, active) => _employeeAdminService.SetActiveAsync(id, active, ct),
                "SetEmployeeActive",
                "angajatul",
                "angajatului",
                ct);

        [HttpPost]
        public Task<IActionResult> SetOrganizationalUnitActive([FromBody] SetActiveRequest request, CancellationToken ct) =>
            HandleSetActiveAsync(
                request,
                (id, active) => _organizationalUnitAdminService.SetActiveAsync(id, active, ct),
                "SetOrganizationalUnitActive",
                "unitatea organizațională",
                "unității organizaționale",
                ct);

        [HttpPost]
        public Task<IActionResult> SetUserActive([FromBody] SetActiveRequest request, CancellationToken ct) =>
            HandleSetActiveAsync(
                request,
                (id, active) => _userAdminService.SetActiveAsync(id, active, ct),
                "SetUserActive",
                "utilizatorul",
                "utilizatorului",
                ct);

        private async Task<IActionResult> HandleSetActiveAsync(
            SetActiveRequest? request,
            Func<int, bool, Task<string?>> serviceCall,
            string auditAction,
            string subjectAccusative,
            string subjectGenitive,
            CancellationToken ct)
        {
            if (request == null)
            {
                return BadRequest(ResponseBase.Error("Cerere invalidă."));
            }

            try
            {
                var validationError = await serviceCall(request.Id, request.Active);
                if (validationError != null)
                {
                    return BadRequest(ResponseBase.Error(validationError));
                }

                var verb = request.Active ? "activat" : "dezactivat";
                if (subjectAccusative.EndsWith("a", StringComparison.Ordinal))
                {
                    verb += "ă";
                }
                await TryLogAsync(auditAction, $"S-a {verb} {subjectAccusative} (Id={request.Id}).");

                return Ok(ResponseBase.Success());
            }
            catch (Exception ex)
            {
                await TryLogAsync(
                    auditAction + "_Error",
                    $"Eroare la modificarea stării {subjectGenitive}.",
                    ex);
                return StatusCode(500, ResponseBase.Error($"Eroare la modificarea stării {subjectGenitive}."));
            }
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
}
