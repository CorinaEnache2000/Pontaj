using Pontaj.Database.Pontaj;

namespace Pontaj.Services.Login;

public interface IRoleService
{
    Task<List<Roles>> GetRolesFromADGroupsAsync(IEnumerable<string> adGroups, CancellationToken ct = default);
}
