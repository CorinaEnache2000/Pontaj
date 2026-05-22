using Pontaj.Database.Pontaj;
using Pontaj.Repositories;
using Pontaj.Services.Logs;

namespace Pontaj.Services.Login;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleLinkRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAppLogger _logger;

    public UserService(
        IUserRepository userRepository,
        IUserRoleRepository userRoleLinkRepository,
        IEmployeeRepository employeeRepository,
        IAppLogger logger)
    {
        _userRepository = userRepository;
        _userRoleLinkRepository = userRoleLinkRepository;
        _employeeRepository = employeeRepository;
        _logger = logger;
    }

    public async Task<AppUsers> GetOrCreateUserAsync(string username, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByUsernameAsync(username, ct);
        if (user == null)
        {
            user = new AppUsers
            {
                Username = username,
                Active = true
            };
            await _userRepository.AddAsync(user, ct);
            await _userRepository.SaveChangesAsync(ct);
        }

        if (!user.EmployeeId.HasValue)
        {
            var employeeId = await _employeeRepository.GetActiveIdByUsernameAsync(username, ct);
            if (employeeId.HasValue)
            {
                user.EmployeeId = employeeId.Value;
                await _userRepository.SaveChangesAsync(ct);

                try
                {
                    await _logger.LogAsync(
                        "User_AutoLink",
                        $"Utilizator '{username}' asociat automat cu angajatul Id={employeeId.Value}.",
                        null,
                        username);
                }
                catch
                {
                }
            }
        }

        return user;
    }

    public async Task SyncUserRolesAsync(int userId, IReadOnlyCollection<Roles> rolesFromAD, CancellationToken ct = default)
    {
        var existing = await _userRoleLinkRepository.GetActiveByUserIdAsync(userId, ct);

        foreach (var link in existing)
        {
            if (rolesFromAD.All(r => r.Id != link.RoleId))
            {
                link.Active = false;
            }
        }

        foreach (var role in rolesFromAD)
        {
            if (existing.All(x => x.RoleId != role.Id))
            {
                await _userRoleLinkRepository.AddAsync(new UserRoles
                {
                    UserId = userId,
                    RoleId = role.Id,
                    Active = true,
                    IsMainRole = false
                }, ct);
            }
        }

        await _userRoleLinkRepository.SaveChangesAsync(ct);
    }
}
