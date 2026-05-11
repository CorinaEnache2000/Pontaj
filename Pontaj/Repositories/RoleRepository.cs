using Microsoft.EntityFrameworkCore;
using Pontaj.Database.Pontaj;

namespace Pontaj.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly PontajContext _context;

    public RoleRepository(PontajContext context)
    {
        _context = context;
    }

    public async Task<List<Roles>> GetActiveByADGroupNamesAsync(IEnumerable<string> adGroupNames, CancellationToken ct = default)
    {
        var groupSet = adGroupNames.ToList();
        return await _context.Roles
            .Where(r => r.Active && r.AdGroupName != null && groupSet.Contains(r.AdGroupName))
            .ToListAsync(ct);
    }

    public Task<Roles?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _context.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);
}
