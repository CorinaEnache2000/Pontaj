using Pontaj.Database.Pontaj;

namespace Pontaj.Repositories;

public interface IUserRoleRepository
{
    Task<List<UserRoles>> GetActiveByUserIdAsync(int userId, CancellationToken ct = default);
    Task AddAsync(UserRoles link, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
