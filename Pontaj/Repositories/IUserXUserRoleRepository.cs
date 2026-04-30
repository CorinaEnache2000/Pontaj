using Pontaj.Database.Pontaj;

namespace Pontaj.Repositories;

public interface IUserXUserRoleRepository
{
    Task<List<UserXUserRole>> GetActiveByUserIdAsync(int userId, CancellationToken ct = default);
    Task AddAsync(UserXUserRole link, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
