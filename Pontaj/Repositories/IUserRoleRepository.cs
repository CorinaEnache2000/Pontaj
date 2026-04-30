using Pontaj.Database.Pontaj;

namespace Pontaj.Repositories;

public interface IUserRoleRepository
{
    Task<List<UserRole>> GetActiveByADGroupNamesAsync(IEnumerable<string> adGroupNames, CancellationToken ct = default);
    Task<UserRole?> GetByIdAsync(int id, CancellationToken ct = default);
}
