using Pontaj.Database.Pontaj;

namespace Pontaj.Services.Login;

public interface IUserService
{
    Task<AppUsers> GetOrCreateUserAsync(string username, CancellationToken ct = default);
    Task SyncUserRolesAsync(int userId, IReadOnlyCollection<Roles> rolesFromAD, CancellationToken ct = default);
}
