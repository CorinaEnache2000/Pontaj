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

    public Task<List<UserRoles>> GetActiveByUserIdAsync(int userId, CancellationToken ct = default) =>
        _context.UserRoles
            .Where(x => x.UserId == userId && x.Active)
            .ToListAsync(ct);

    public Task<List<UserRoles>> GetAllByUserIdAsync(int userId, CancellationToken ct = default) =>
        _context.UserRoles
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);

    public async Task AddAsync(UserRoles link, CancellationToken ct = default)
    {
        await _context.UserRoles.AddAsync(link, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
