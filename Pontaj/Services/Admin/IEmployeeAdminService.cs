using Pontaj.Models.Admin.Employees;

namespace Pontaj.Services.Admin;

public interface IEmployeeAdminService
{
    Task<EmployeesViewModel> GetEmployeesViewModelAsync(CancellationToken ct = default);

    Task<EmployeeDetail?> GetEmployeeDetailAsync(int id, CancellationToken ct = default);

    Task<int> SyncEmployeesAsync(CancellationToken ct = default);

    Task<string?> SetActiveAsync(int id, bool active, CancellationToken ct = default);
}
