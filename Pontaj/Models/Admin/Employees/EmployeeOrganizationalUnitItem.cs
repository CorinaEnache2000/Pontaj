namespace Pontaj.Models.Admin.Employees;

// One organizational unit an employee belongs to. Name is resolved from the
// TextResources table (OU PublicNameKey + OU DefaultLanguageId).
public class EmployeeOrganizationalUnitItem
{
    public int OrganizationalUnitId { get; set; }

    public string Name { get; set; } = null!;
}
