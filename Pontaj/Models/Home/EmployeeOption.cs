namespace Pontaj.Models.Home;

public class EmployeeOption
{
    public int Id { get; set; }

    public string LastName { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string DisplayName => $"{LastName} {FirstName}".Trim();
}
