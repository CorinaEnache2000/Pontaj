using Microsoft.EntityFrameworkCore;
using Pontaj.Database.Pontaj;
using Pontaj.Models.Admin.Users;

namespace Pontaj.Services.Admin;

// Read-only assembly of the Users admin page models, same shape as the
// Employees / OrganizationalUnits admin services: a flat list for the sidebar,
// per-user detail (General / Roluri) loaded on demand.
public class UserAdminService : IUserAdminService
{
    private readonly PontajContext _context;

    public UserAdminService(PontajContext context)
    {
        _context = context;
    }

    public async Task<UsersViewModel> GetUsersViewModelAsync(CancellationToken ct = default)
    {
        var users = await _context.AppUsers
            .OrderBy(u => u.Username)
            .Select(u => new UserListItem
            {
                Id = u.Id,
                Username = u.Username,
                Active = u.Active
            })
            .ToListAsync(ct);

        return new UsersViewModel { Users = users };
    }

    public async Task<UserDetail?> GetDetailAsync(int id, CancellationToken ct = default)
    {
        return await _context.AppUsers
            .Where(u => u.Id == id)
            .Select(u => new UserDetail
            {
                Id = u.Id,
                Username = u.Username,
                EmployeeName = u.Employee == null
                    ? null
                    : u.Employee.LastName + " " + u.Employee.FirstName,
                Roles = u.UserRoles
                    .OrderBy(ur => ur.Role.Name)
                    .Select(ur => ur.Role.Name)
                    .ToList(),
                Active = u.Active
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<UserRoleItem>> GetRolesAsync(int id, CancellationToken ct = default)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == id)
            .OrderBy(ur => ur.Role.Name)
            .Select(ur => new UserRoleItem
            {
                RoleId = ur.RoleId,
                RoleName = ur.Role.Name,
                IsMainRole = ur.IsMainRole,
                Active = ur.Active
            })
            .ToListAsync(ct);
    }
}
