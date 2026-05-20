using Pontaj.Models.Admin.Users;

namespace Pontaj.Services.Admin;

public interface IUserAdminService
{
    Task<UsersViewModel> GetUsersViewModelAsync(CancellationToken ct = default);

    Task<UserDetail?> GetDetailAsync(int id, CancellationToken ct = default);

    Task<List<UserRoleItem>> GetRolesAsync(int id, CancellationToken ct = default);

    Task<string?> SetActiveAsync(int id, bool active, CancellationToken ct = default);
}
