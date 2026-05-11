using Pontaj.Database.Pontaj;

namespace Pontaj.Repositories;

public interface IRoleRepository
{
    Task<List<Roles>> GetActiveByADGroupNamesAsync(IEnumerable<string> adGroupNames, CancellationToken ct = default);
    Task<Roles?> GetByIdAsync(int id, CancellationToken ct = default);
}
