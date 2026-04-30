using Microsoft.EntityFrameworkCore;
using Pontaj.Database.Pontaj;

namespace Pontaj.Repositories;

public class UserRoleRepository : IUserRoleRepository
{
    private readonly PontajContext _context;

    public UserRoleRepository(PontajContext context)
    {
        _context = context;
    }

    public async Task<List<UserRole>> GetActiveByADGroupNamesAsync(IEnumerable<string> adGroupNames, CancellationToken ct = default)
    {
        var groupSet = adGroupNames.ToList();
        return await _context.UserRoles
            .Where(r => r.Active && r.ADGroupName != null && groupSet.Contains(r.ADGroupName))
            .ToListAsync(ct);
    }

    public Task<UserRole?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _context.UserRoles.FirstOrDefaultAsync(r => r.Id == id, ct);
}
