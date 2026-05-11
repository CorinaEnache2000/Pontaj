using Pontaj.Database.Pontaj;

namespace Pontaj.Repositories;

public interface IUserRepository
{
    Task<AppUsers?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<AppUsers?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(AppUsers user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
