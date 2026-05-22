using Pontaj.Database.Pontaj;

namespace Pontaj.Repositories;

public interface IEmployeeRepository
{
    Task<Employees?> GetActiveByBadgeAsync(string badge, CancellationToken ct = default);

    Task<int?> GetPrimaryActiveOrganizationalUnitIdAsync(int employeeId, CancellationToken ct = default);

    Task<int?> GetActiveIdByUsernameAsync(string username, CancellationToken ct = default);
}
