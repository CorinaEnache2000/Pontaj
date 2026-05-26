using Microsoft.EntityFrameworkCore;
using Pontaj.Database.Pontaj;
using Pontaj.Models.Home;
using Pontaj.Services.Scan;

namespace Pontaj.Repositories;

public class ScanRepository : IScanRepository
{
    private readonly PontajContext _context;

    public ScanRepository(PontajContext context)
    {
        _context = context;
    }

    public async Task<(List<ScanListItem> Items, int Total)> GetPageAsync(
        ScanPageRequest request,
        ScanScope scope,
        CancellationToken ct = default)
    {
        var q = _context.Punches.AsNoTracking().AsQueryable();

        if (!scope.IsAdmin)
        {
            if (scope.AllowedOuIds != null)
            {
                var allowedOuIds = scope.AllowedOuIds;
                var selfEmployeeId = scope.SelfEmployeeId;
                q = q.Where(p =>
                    (selfEmployeeId.HasValue && p.EmployeeId == selfEmployeeId.Value)
                    || p.Employee.EmployeeOrganizationalUnits.Any(eou =>
                        eou.Active && allowedOuIds.Contains(eou.OrganizationalUnitId)));
            }
            else if (scope.SelfEmployeeId.HasValue)
            {
                var selfEmployeeId = scope.SelfEmployeeId.Value;
                q = q.Where(p => p.EmployeeId == selfEmployeeId);
            }
            else
            {
                return (new List<ScanListItem>(), 0);
            }
        }

        if (request.EmployeeIds != null && request.EmployeeIds.Count > 0)
        {
            var ids = request.EmployeeIds;
            q = q.Where(p => ids.Contains(p.EmployeeId));
        }
        if (request.WorkStationIds != null && request.WorkStationIds.Count > 0)
        {
            var ids = request.WorkStationIds;
            q = q.Where(p => p.WorkStationId.HasValue && ids.Contains(p.WorkStationId.Value));
        }
        if (request.OrganizationalUnitIds != null && request.OrganizationalUnitIds.Count > 0)
        {
            var ids = request.OrganizationalUnitIds;
            q = q.Where(p => p.Employee.EmployeeOrganizationalUnits.Any(eou =>
                eou.Active && ids.Contains(eou.OrganizationalUnitId)));
        }
        if (request.From.HasValue)
        {
            var from = request.From.Value;
            q = q.Where(p => p.InsertedDate >= from);
        }
        if (request.To.HasValue)
        {
            var to = request.To.Value;
            q = q.Where(p => p.InsertedDate <= to);
        }

        var total = await q.CountAsync(ct);

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 50 : (request.PageSize > 200 ? 200 : request.PageSize);

        var items = await q
            .OrderByDescending(p => p.InsertedMoment)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ScanListItem
            {
                Id = p.Id,
                EmployeeId = p.EmployeeId,
                EmployeeLastName = p.Employee.LastName,
                EmployeeFirstName = p.Employee.FirstName,
                Mark = p.Employee.Mark,
                Badge = p.Badge,
                InOut = p.InOut,
                InsertedMoment = p.InsertedMoment,
                WorkStationId = p.WorkStationId,
                WorkStationName = p.WorkStation != null ? p.WorkStation.NameKey : null,
                Ip = p.Ip
            })
            .ToListAsync(ct);

        if (items.Count > 0)
        {
            var employeeIds = items.Select(i => i.EmployeeId).Distinct().ToList();

            var ouNames = await _context.EmployeeOrganizationalUnits
                .Where(eou => eou.Active && employeeIds.Contains(eou.EmployeeId))
                .OrderBy(eou => eou.Id)
                .Select(eou => new
                {
                    eou.EmployeeId,
                    Name = _context.TextResources
                        .Where(tr => tr.ResourceKey == eou.OrganizationalUnit.PublicNameKey
                                  && tr.LanguageId == eou.OrganizationalUnit.DefaultLanguageId)
                        .Select(tr => tr.Value)
                        .FirstOrDefault() ?? eou.OrganizationalUnit.PublicNameKey
                })
                .ToListAsync(ct);

            var namesByEmployee = ouNames
                .GroupBy(x => x.EmployeeId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

            foreach (var item in items)
            {
                if (namesByEmployee.TryGetValue(item.EmployeeId, out var names))
                {
                    item.EmployeeOrganizationalUnitNames = names;
                }
            }
        }

        return (items, total);
    }

    public Task<Punches?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _context.Punches.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task InsertAsync(Punches punch, CancellationToken ct = default)
    {
        await _context.Punches.AddAsync(punch, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);

    public Task DeleteAsync(Punches punch, CancellationToken ct = default)
    {
        _context.Punches.Remove(punch);
        return Task.CompletedTask;
    }
}
