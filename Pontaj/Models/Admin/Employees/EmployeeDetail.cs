namespace Pontaj.Models.Admin.Employees;

public class EmployeeDetail
{
    public int Id { get; set; }

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? Pin { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? Badge { get; set; }

    public string? Code { get; set; }

    public string? Username { get; set; }

    public bool Active { get; set; }

    public List<EmployeeOrganizationalUnitItem> OrganizationalUnits { get; set; } = new();
}
