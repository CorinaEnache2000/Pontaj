using Microsoft.EntityFrameworkCore;
using Pontaj.Database.Pontaj;

namespace Pontaj.Repositories;

public class UserRepository : IUserRepository
{
    private readonly PontajContext _context;

    public UserRepository(PontajContext context)
    {
        _context = context;
    }

    public Task<AppUser?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        _context.AppUsers.FirstOrDefaultAsync(u => u.Username == username, ct);

    public Task<AppUser?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _context.AppUsers.FirstOrDefaultAsync(u => u.ID == id, ct);

    public async Task AddAsync(AppUser user, CancellationToken ct = default)
    {
        await _context.AppUsers.AddAsync(user, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
