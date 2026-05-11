using Pontaj.Database.Pontaj;
using Pontaj.Repositories;

namespace Pontaj.Services.Login;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;

    public RoleService(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public Task<List<Roles>> GetRolesFromADGroupsAsync(IEnumerable<string> adGroups, CancellationToken ct = default) =>
        _roleRepository.GetActiveByADGroupNamesAsync(adGroups, ct);
}
