namespace Pontaj.Models.Admin.Employees;

// Right-side detail panel for a single selected employee, plus the
// organizational units the employee is assigned to.
public class EmployeeDetail
{
    public int Id { get; set; }

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? Pin { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? Badge { get; set; }

    public string? Code { get; set; }

    public bool Active { get; set; }

    // The employee's organizational units (id + resolved display name).
    public List<EmployeeOrganizationalUnitItem> OrganizationalUnits { get; set; } = new();
}
