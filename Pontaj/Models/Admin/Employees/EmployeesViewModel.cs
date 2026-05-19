namespace Pontaj.Models.Admin.Employees;

// Backing model for the Employees admin page. Holds the left-side list; the
// right-side detail is fetched per-row via a separate partial-view request.
public class EmployeesViewModel
{
    public List<EmployeeListItem> Employees { get; set; } = new();
}
