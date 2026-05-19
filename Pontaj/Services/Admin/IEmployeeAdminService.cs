using Pontaj.Models.Admin.Employees;

namespace Pontaj.Services.Admin;

public interface IEmployeeAdminService
{
    Task<EmployeesViewModel> GetEmployeesViewModelAsync(CancellationToken ct = default);

    Task<EmployeeDetail?> GetEmployeeDetailAsync(int id, CancellationToken ct = default);

    // Returns the number of employee records processed by the sync.
    Task<int> SyncEmployeesAsync(CancellationToken ct = default);
}
