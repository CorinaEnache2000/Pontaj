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

        await ReconcileEmployeeLinkAsync(user, username, ct);

        return user;
    }

    private async Task ReconcileEmployeeLinkAsync(AppUsers user, string username, CancellationToken ct)
    {
        // Already linked: keep Employees.Username in sync so the Angajați page reflects the link
        // (heals accounts linked before write-back existed). _userRepository and _employeeRepository
        // share the same scoped PontajContext, so one SaveChanges flushes both entities.
        if (user.EmployeeId.HasValue)
        {
            var filled = await _employeeRepository.SetUsernameIfEmptyAsync(user.EmployeeId.Value, username, ct);
            if (filled)
            {
                await _userRepository.SaveChangesAsync(ct);
            }
            return;
        }

        // 1. Authoritative path: an admin has explicitly set Employees.Username for this account.
        var employeeId = await _employeeRepository.GetActiveIdByUsernameAsync(username, ct);
        string linkReason = "după nume de utilizator";

        // 2. Fallback: unambiguous name match against unassigned, unlinked employees.
        //    Promote the match by recording Employees.Username, so it shows on the Angajați page
        //    and becomes the authoritative key on subsequent logins.
        if (!employeeId.HasValue)
        {
            var candidates = await _employeeRepository.GetActiveUnassignedNamesAsync(ct);
            employeeId = EmployeeNameMatcher.Match(username, candidates);
            if (employeeId.HasValue)
            {
                linkReason = "după nume";
                await _employeeRepository.SetUsernameIfEmptyAsync(employeeId.Value, username, ct);
            }
        }

        if (!employeeId.HasValue)
        {
            return;
        }

        user.EmployeeId = employeeId.Value;
        await _userRepository.SaveChangesAsync(ct);

        try
        {
            await _logger.LogAsync(
                "User_AutoLink",
                $"Utilizator '{username}' asociat automat cu angajatul Id={employeeId.Value} ({linkReason}).",
                null,
                username);
        }
        catch
        {
        }
    }

    public async Task SyncUserRolesAsync(int userId, IReadOnlyCollection<Roles> rolesFromAD, CancellationToken ct = default)
    {
        // Load ALL links (active and inactive) so a previously-revoked role that AD grants
        // again is reactivated in place rather than inserted as a duplicate row.
        var existing = await _userRoleLinkRepository.GetAllByUserIdAsync(userId, ct);
        var adRoleIds = rolesFromAD.Select(r => r.Id).ToHashSet();

        foreach (var link in existing)
        {
            var shouldBeActive = adRoleIds.Contains(link.RoleId);
            if (link.Active != shouldBeActive)
            {
                link.Active = shouldBeActive;
            }
            if (!shouldBeActive)
            {
                // Never keep the main-role flag on a revoked role.
                link.IsMainRole = false;
            }
        }

        var activeLinks = existing.Where(x => x.Active).ToList();

        foreach (var role in rolesFromAD)
        {
            if (existing.All(x => x.RoleId != role.Id))
            {
                var newLink = new UserRoles
                {
                    UserId = userId,
                    RoleId = role.Id,
                    Active = true,
                    IsMainRole = false
                };
                await _userRoleLinkRepository.AddAsync(newLink, ct);
                activeLinks.Add(newLink);
            }
        }

        // Guarantee exactly one active role is flagged as main. Respect an existing manual
        // choice; otherwise pick the lowest RoleId (highest-privilege) deterministically.
        if (activeLinks.Count > 0 && !activeLinks.Any(x => x.IsMainRole))
        {
            var primary = activeLinks.OrderBy(x => x.RoleId).First();
            primary.IsMainRole = true;
        }

        await _userRoleLinkRepository.SaveChangesAsync(ct);
    }
}
