using Pontaj.Database.Pontaj;

namespace Pontaj.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<AppUser?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(AppUser user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
