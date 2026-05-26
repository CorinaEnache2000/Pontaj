using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Pontaj.Database.Pontaj;
using Pontaj.Models.Home;
using Pontaj.Repositories;
using Pontaj.Services.Logs;

namespace Pontaj.Services.Scan;

public class ScanService : IScanService
{
    private const string AdminRole = "ADMINISTRATOR";
    private const string DirectorRole = "DIRECTOR";

    private readonly PontajContext _context;
    private readonly IScanRepository _scanRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IOuHierarchyService _ouHierarchy;
    private readonly IAppLogger _logger;

    public ScanService(
        PontajContext context,
        IScanRepository scanRepository,
        IEmployeeRepository employeeRepository,
        IOuHierarchyService ouHierarchy,
        IAppLogger logger)
    {
        _context = context;
        _scanRepository = scanRepository;
        _employeeRepository = employeeRepository;
        _ouHierarchy = ouHierarchy;
        _logger = logger;
    }

    public async Task<ScanScope> ResolveScopeAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        var isAdmin = user.IsInRole(AdminRole);
        var isDirector = user.IsInRole(DirectorRole);

        int? selfEmployeeId = null;
        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idClaim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var appUserId))
        {
            selfEmployeeId = await _context.AppUsers
                .Where(u => u.Id == appUserId)
                .Select(u => u.EmployeeId)
                .FirstOrDefaultAsync(ct);
        }

        if (isAdmin)
        {
            return new ScanScope
            {
                IsAdmin = true,
                IsDirector = isDirector,
                SelfEmployeeId = selfEmployeeId,
                AllowedOuIds = null
            };
        }

        if (isDirector && selfEmployeeId.HasValue)
        {
            var rootIds = await _context.EmployeeOrganizationalUnits
                .Where(eou => eou.Active && eou.EmployeeId == selfEmployeeId.Value)
                .Select(eou => eou.OrganizationalUnitId)
                .ToListAsync(ct);

            var allowed = await _ouHierarchy.GetDescendantIdsAsync(rootIds, ct);

            return new ScanScope
            {
                IsAdmin = false,
                IsDirector = true,
                SelfEmployeeId = selfEmployeeId,
                AllowedOuIds = allowed
            };
        }

        return new ScanScope
        {
            IsAdmin = false,
            IsDirector = false,
            SelfEmployeeId = selfEmployeeId,
            AllowedOuIds = null
        };
    }

    public async Task<ScansIndexViewModel> BuildIndexViewModelAsync(ScanScope scope, CancellationToken ct = default)
    {
        var vm = new ScansIndexViewModel
        {
            IsLinked = scope.SelfEmployeeId.HasValue || scope.IsAdmin,
            CanEdit = scope.CanEdit,
            ShowOtherEmployeeFilters = scope.CanEdit,
            ShowIpColumn = scope.CanEdit
        };

        if (!vm.ShowOtherEmployeeFilters)
        {
            return vm;
        }

        var employeeQuery = _context.Employees
            .Where(e => e.Active);

        if (!scope.IsAdmin && scope.AllowedOuIds != null)
        {
            var allowed = scope.AllowedOuIds;
            var self = scope.SelfEmployeeId;
            employeeQuery = employeeQuery.Where(e =>
                (self.HasValue && e.Id == self.Value)
                || e.EmployeeOrganizationalUnits.Any(eou => eou.Active && allowed.Contains(eou.OrganizationalUnitId)));
        }

        vm.Employees = await employeeQuery
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Select(e => new EmployeeOption
            {
                Id = e.Id,
                LastName = e.LastName,
                FirstName = e.FirstName
            })
            .ToListAsync(ct);

        vm.WorkStations = await _context.WorkStations
            .Where(w => w.Active)
            .OrderBy(w => w.NameKey)
            .Select(w => new WorkStationOption
            {
                Id = w.Id,
                Name = w.NameKey
            })
            .ToListAsync(ct);

        var ouQuery = _context.OrganizationalUnits.Where(o => o.Active);
        if (!scope.IsAdmin && scope.AllowedOuIds != null)
        {
            var allowed = scope.AllowedOuIds;
            ouQuery = ouQuery.Where(o => allowed.Contains(o.Id));
        }

        var flatOus = await ouQuery
            .Select(o => new
            {
                o.Id,
                ParentId = o.ParentOrganizationalUnitId,
                Name = _context.TextResources
                    .Where(tr => tr.ResourceKey == o.PublicNameKey
                              && tr.LanguageId == o.DefaultLanguageId)
                    .Select(tr => tr.Value)
                    .FirstOrDefault() ?? o.PublicNameKey
            })
            .ToListAsync(ct);

        vm.OrganizationalUnits = flatOus
            .Select(o => new OrganizationalUnitOption { Id = o.Id, Name = o.Name })
            .OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var nodesById = flatOus.ToDictionary(
            o => o.Id,
            o => new OuTreeNode { Id = o.Id, ParentId = o.ParentId, Name = o.Name });

        var roots = new List<OuTreeNode>();
        foreach (var node in nodesById.Values)
        {
            if (node.ParentId.HasValue
                && node.ParentId.Value != node.Id
                && nodesById.TryGetValue(node.ParentId.Value, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        SortOuTree(roots);
        vm.OrganizationalUnitTree = roots;

        return vm;
    }

    private static void SortOuTree(List<OuTreeNode> nodes)
    {
        nodes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        foreach (var node in nodes)
        {
            SortOuTree(node.Children);
        }
    }

    public Task<(List<ScanListItem> Items, int Total)> GetPageAsync(
        ScanPageRequest request,
        ScanScope scope,
        CancellationToken ct = default) =>
        _scanRepository.GetPageAsync(request, scope, ct);

    public async Task<(string? ValidationError, Punches? Created)> CreateAsync(
        ScanCreateRequest request,
        ScanScope scope,
        string actorUsername,
        CancellationToken ct = default)
    {
        if (!scope.CanEdit)
        {
            return ("Nu aveți drept de modificare.", null);
        }
        if (request.EmployeeId <= 0)
        {
            return ("Selectați un angajat.", null);
        }
        if (request.Moment == default)
        {
            return ("Selectați momentul.", null);
        }

        var employee = await _context.Employees
            .Where(e => e.Id == request.EmployeeId)
            .Select(e => new { e.Id, e.Badge, e.Active })
            .FirstOrDefaultAsync(ct);
        if (employee == null)
        {
            return ("Angajatul nu există.", null);
        }
        if (!employee.Active)
        {
            return ("Angajatul este inactiv.", null);
        }
        if (string.IsNullOrWhiteSpace(employee.Badge))
        {
            return ("Angajatul nu are marca completată.", null);
        }

        if (!await IsEmployeeInScopeAsync(employee.Id, scope, ct))
        {
            return ("Acest angajat este în afara ariei dumneavoastră.", null);
        }

        int? targetOuId = request.OrganizationalUnitId;
        if (!targetOuId.HasValue)
        {
            targetOuId = await _employeeRepository.GetPrimaryActiveOrganizationalUnitIdAsync(employee.Id, ct);
        }

        if (request.WorkStationId.HasValue)
        {
            var wsExists = await _context.WorkStations.AnyAsync(w => w.Id == request.WorkStationId.Value, ct);
            if (!wsExists)
            {
                return ("Stația de lucru selectată nu există.", null);
            }
        }
        if (targetOuId.HasValue)
        {
            var ouExists = await _context.OrganizationalUnits.AnyAsync(o => o.Id == targetOuId.Value, ct);
            if (!ouExists)
            {
                return ("Unitatea organizațională selectată nu există.", null);
            }
        }

        var moment = request.Moment;
        var punch = new Punches
        {
            EmployeeId = employee.Id,
            Badge = employee.Badge!,
            InOut = request.InOut,
            InsertedDate = DateOnly.FromDateTime(moment),
            InsertedTime = TimeOnly.FromDateTime(moment),
            InsertedMoment = moment,
            WorkStationId = request.WorkStationId,
            OrganizationalUnitId = targetOuId,
            Ip = null
        };

        await _scanRepository.InsertAsync(punch, ct);
        await _scanRepository.SaveChangesAsync(ct);

        try
        {
            await _logger.LogAsync(
                "Punch_Create",
                $"Punch_Create id={punch.Id} after={Snapshot(punch)}",
                null,
                actorUsername);
        }
        catch
        {
        }

        return (null, punch);
    }

    public async Task<(string? ValidationError, Punches? Updated)> UpdateAsync(
        ScanUpdateRequest request,
        ScanScope scope,
        string actorUsername,
        CancellationToken ct = default)
    {
        if (!scope.CanEdit)
        {
            return ("Nu aveți drept de modificare.", null);
        }
        if (request.Moment == default)
        {
            return ("Selectați momentul.", null);
        }

        var punch = await _scanRepository.GetByIdAsync(request.Id, ct);
        if (punch == null)
        {
            return ("Pontajul nu există.", null);
        }

        if (!await IsEmployeeInScopeAsync(punch.EmployeeId, scope, ct))
        {
            return ("Acest pontaj este în afara ariei dumneavoastră.", null);
        }

        int? targetOuId = request.OrganizationalUnitId;
        if (!targetOuId.HasValue)
        {
            targetOuId = await _employeeRepository.GetPrimaryActiveOrganizationalUnitIdAsync(punch.EmployeeId, ct);
        }

        if (request.WorkStationId.HasValue)
        {
            var wsExists = await _context.WorkStations.AnyAsync(w => w.Id == request.WorkStationId.Value, ct);
            if (!wsExists)
            {
                return ("Stația de lucru selectată nu există.", null);
            }
        }
        if (targetOuId.HasValue)
        {
            var ouExists = await _context.OrganizationalUnits.AnyAsync(o => o.Id == targetOuId.Value, ct);
            if (!ouExists)
            {
                return ("Unitatea organizațională selectată nu există.", null);
            }
        }

        var before = Snapshot(punch);
        var moment = request.Moment;
        punch.InOut = request.InOut;
        punch.InsertedMoment = moment;
        punch.InsertedDate = DateOnly.FromDateTime(moment);
        punch.InsertedTime = TimeOnly.FromDateTime(moment);
        punch.WorkStationId = request.WorkStationId;
        punch.OrganizationalUnitId = targetOuId;

        await _scanRepository.SaveChangesAsync(ct);

        try
        {
            await _logger.LogAsync(
                "Punch_Update",
                $"Punch_Update id={punch.Id} before={before} after={Snapshot(punch)}",
                null,
                actorUsername);
        }
        catch
        {
        }

        return (null, punch);
    }

    public async Task<string?> DeleteAsync(
        long id,
        ScanScope scope,
        string actorUsername,
        CancellationToken ct = default)
    {
        if (!scope.CanEdit)
        {
            return "Nu aveți drept de modificare.";
        }

        var punch = await _scanRepository.GetByIdAsync(id, ct);
        if (punch == null)
        {
            return "Pontajul nu există.";
        }

        if (!await IsEmployeeInScopeAsync(punch.EmployeeId, scope, ct))
        {
            return "Acest pontaj este în afara ariei dumneavoastră.";
        }

        var snapshot = Snapshot(punch);

        await _scanRepository.DeleteAsync(punch, ct);
        await _scanRepository.SaveChangesAsync(ct);

        try
        {
            await _logger.LogAsync(
                "Punch_Delete",
                $"Punch_Delete id={id} before={snapshot}",
                null,
                actorUsername);
        }
        catch
        {
        }

        return null;
    }

    private async Task<bool> IsEmployeeInScopeAsync(int employeeId, ScanScope scope, CancellationToken ct)
    {
        if (scope.IsAdmin)
        {
            return true;
        }
        if (scope.SelfEmployeeId.HasValue && scope.SelfEmployeeId.Value == employeeId)
        {
            return true;
        }
        if (scope.AllowedOuIds == null || scope.AllowedOuIds.Count == 0)
        {
            return false;
        }
        var allowed = scope.AllowedOuIds;
        return await _context.EmployeeOrganizationalUnits
            .AnyAsync(eou => eou.EmployeeId == employeeId
                          && eou.Active
                          && allowed.Contains(eou.OrganizationalUnitId), ct);
    }

    private static string Snapshot(Punches p)
    {
        var sb = new StringBuilder();
        sb.Append("{emp:").Append(p.EmployeeId);
        sb.Append(",inOut:").Append(p.InOut ? "true" : "false");
        sb.Append(",moment:\"").Append(p.InsertedMoment.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)).Append('"');
        sb.Append(",ws:").Append(p.WorkStationId.HasValue ? p.WorkStationId.Value.ToString(CultureInfo.InvariantCulture) : "null");
        sb.Append(",ou:").Append(p.OrganizationalUnitId.HasValue ? p.OrganizationalUnitId.Value.ToString(CultureInfo.InvariantCulture) : "null");
        sb.Append('}');
        return sb.ToString();
    }
}
