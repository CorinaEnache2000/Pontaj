using Pontaj.Database.Pontaj;

namespace Pontaj.Services.Login;

public interface IUserService
{
    Task<AppUser> GetOrCreateUserAsync(string username, CancellationToken ct = default);
    Task SyncUserRolesAsync(int userId, IReadOnlyCollection<UserRole> rolesFromAD, CancellationToken ct = default);
}
