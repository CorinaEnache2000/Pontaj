using Microsoft.EntityFrameworkCore;
using Pontaj.Database.Pontaj;

namespace Pontaj.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly PontajContext _context;

    public EmployeeRepository(PontajContext context)
    {
        _context = context;
    }

    public Task<Employees?> GetActiveByBadgeAsync(string badge, CancellationToken ct = default) =>
        _context.Employees.FirstOrDefaultAsync(e => e.Active && e.Badge == badge, ct);

    public Task<int?> GetPrimaryActiveOrganizationalUnitIdAsync(int employeeId, CancellationToken ct = default) =>
        _context.EmployeeOrganizationalUnits
            .Where(eou => eou.EmployeeId == employeeId && eou.Active)
            .OrderBy(eou => eou.Id)
            .Select(eou => (int?)eou.OrganizationalUnitId)
            .FirstOrDefaultAsync(ct);

    public Task<int?> GetActiveIdByUsernameAsync(string username, CancellationToken ct = default) =>
        _context.Employees
            .Where(e => e.Active && e.Username == username)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync(ct);

    public Task<List<EmployeeNameRow>> GetActiveUnassignedNamesAsync(CancellationToken ct = default) =>
        _context.Employees
            .Where(e => e.Active
                     && e.Username == null
                     && !_context.AppUsers.Any(u => u.EmployeeId == e.Id))
            .Select(e => new EmployeeNameRow
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName
            })
            .ToListAsync(ct);

    public async Task<bool> SetUsernameIfEmptyAsync(int employeeId, string username, CancellationToken ct = default)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee == null || !string.IsNullOrWhiteSpace(employee.Username))
        {
            return false;
        }

        var taken = await _context.Employees.AnyAsync(e => e.Id != employeeId && e.Username == username, ct);
        if (taken)
        {
            return false;
        }

        employee.Username = username;
        return true;
    }
}
