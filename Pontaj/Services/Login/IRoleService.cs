using Pontaj.Database.Pontaj;

namespace Pontaj.Services.Login;

public interface IRoleService
{
    Task<List<UserRole>> GetRolesFromADGroupsAsync(IEnumerable<string> adGroups, CancellationToken ct = default);
}
