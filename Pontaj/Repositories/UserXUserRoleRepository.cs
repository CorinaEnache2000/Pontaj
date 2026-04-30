using Microsoft.EntityFrameworkCore;
using Pontaj.Database.Pontaj;

namespace Pontaj.Repositories;

public class UserXUserRoleRepository : IUserXUserRoleRepository
{
    private readonly PontajContext _context;

    public UserXUserRoleRepository(PontajContext context)
    {
        _context = context;
    }

    public Task<List<UserXUserRole>> GetActiveByUserIdAsync(int userId, CancellationToken ct = default) =>
        _context.UserXUserRoles
            .Where(x => x.UserId == userId && x.Active)
            .ToListAsync(ct);

    public async Task AddAsync(UserXUserRole link, CancellationToken ct = default)
    {
        await _context.UserXUserRoles.AddAsync(link, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
