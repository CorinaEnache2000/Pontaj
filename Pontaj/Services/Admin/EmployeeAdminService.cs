using Microsoft.EntityFrameworkCore;
using Pontaj.Database.Pontaj;
using Pontaj.Models.Admin.Employees;

namespace Pontaj.Services.Admin;

// Read-only assembly of the Employees admin page models. Mirrors the ManVan
// UserService pattern: a flat list for the sidebar, a richer per-row detail
// loaded on demand. No writes, so the deferred-SaveChanges repo rule is N/A.
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
                Active = e.Active,
                // Resolve each OU's display name from TextResources at the OU's
                // default language; fall back to the raw key if no resource row.
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

        // Sort by the resolved display name (not the raw key) so the list is
        // alphabetical by what the user actually sees.
        detail?.OrganizationalUnits.Sort(
            (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

        return detail;
    }

    public async Task<int> SyncEmployeesAsync(CancellationToken ct = default)
    {
        // TODO: wire the real employee source here (Active Directory bulk
        // enumeration, or a second read-only HR/DW DbContext — decision pending).
        // The button, spinner, endpoint, audit log and list refresh are all
        // fully wired like the ManVan SaleAgents sync; only this data-pull is a
        // no-op stub for now. Returns the count of records processed.
        await Task.CompletedTask;
        return 0;
    }
}
