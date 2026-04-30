using Pontaj.Database.Pontaj;
using Pontaj.Repositories;

namespace Pontaj.Services.Login;

public class RoleService : IRoleService
{
    private readonly IUserRoleRepository _roleRepository;

    public RoleService(IUserRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public Task<List<UserRole>> GetRolesFromADGroupsAsync(IEnumerable<string> adGroups, CancellationToken ct = default) =>
        _roleRepository.GetActiveByADGroupNamesAsync(adGroups, ct);
}
