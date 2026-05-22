using Microsoft.EntityFrameworkCore;
using Pontaj.Database.Pontaj;
using Pontaj.Models.Admin.Employees;

namespace Pontaj.Services.Admin;

public class EmployeeAdminService : IEmployeeAdminService
{
    private readonly PontajContext _context;

    public EmployeeAdminService(PontajContext context)
    {
        _context = context;
    }

    public async Task<EmployeesViewModel> GetEmployeesViewModelAsync(CancellationToken ct = default)
    {
        var employees = await _context.Employees
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Select(e => new EmployeeListItem
            {
                Id = e.Id,
                LastName = e.LastName,
                FirstName = e.FirstName,
                Active = e.Active
            })
            .ToListAsync(ct);

        return new EmployeesViewModel { Employees = employees };
    }

    public async Task<EmployeeDetail?> GetEmployeeDetailAsync(int id, CancellationToken ct = default)
    {
        var detail = await _context.Employees
            .Where(e => e.Id == id)
            .Select(e => new EmployeeDetail
            {
                Id = e.Id,
                LastName = e.LastName,
                FirstName = e.FirstName,
                Pin = e.Pin,
                BirthDate = e.BirthDate,
                Badge = e.Badge,
                Code = e.Code,
                Username = e.Username,
                Active = e.Active,
                OrganizationalUnits = e.EmployeeOrganizationalUnits
                    .Select(eou => new EmployeeOrganizationalUnitItem
                    {
                        OrganizationalUnitId = eou.OrganizationalUnitId,
                        Name = _context.TextResources
                            .Where(tr => tr.ResourceKey == eou.OrganizationalUnit.PublicNameKey
                                      && tr.LanguageId == eou.OrganizationalUnit.DefaultLanguageId)
                            .Select(tr => tr.Value)
                            .FirstOrDefault() ?? eou.OrganizationalUnit.PublicNameKey
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        detail?.OrganizationalUnits.Sort(
            (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

        return detail;
    }

    public async Task<string?> SetActiveAsync(int id, bool active, CancellationToken ct = default)
    {
        var entity = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity == null)
        {
            return "Angajatul nu există.";
        }
        entity.Active = active;
        await _context.SaveChangesAsync(ct);
        return null;
    }

    public async Task<int> SyncEmployeesAsync(CancellationToken ct = default)
    {
        // TODO: wire the real employee source (AD bulk enumeration or read-only HR/DW context).
        await Task.CompletedTask;
        return 0;
    }
}
