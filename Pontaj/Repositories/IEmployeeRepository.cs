using Pontaj.Database.Pontaj;

namespace Pontaj.Repositories;

public sealed class EmployeeNameRow
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public interface IEmployeeRepository
{
    Task<Employees?> GetActiveByBadgeAsync(string badge, CancellationToken ct = default);

    Task<int?> GetPrimaryActiveOrganizationalUnitIdAsync(int employeeId, CancellationToken ct = default);

    Task<int?> GetActiveIdByUsernameAsync(string username, CancellationToken ct = default);

    Task<List<EmployeeNameRow>> GetActiveUnassignedNamesAsync(CancellationToken ct = default);

    /// <summary>
    /// Records the AD username on the employee only if it is currently empty and not already
    /// taken by another employee. Does not save — the caller flushes the shared context.
    /// Returns true if the value was changed.
    /// </summary>
    Task<bool> SetUsernameIfEmptyAsync(int employeeId, string username, CancellationToken ct = default);
}
