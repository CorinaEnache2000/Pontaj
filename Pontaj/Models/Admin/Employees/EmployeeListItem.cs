namespace Pontaj.Models.Admin.Employees;

// One row in the left-side employees list. Kept lightweight on purpose — the
// full detail is loaded on demand when a row is clicked.
public class EmployeeListItem
{
    public int Id { get; set; }

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public bool Active { get; set; }
}
