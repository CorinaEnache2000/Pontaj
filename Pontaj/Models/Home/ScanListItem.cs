namespace Pontaj.Models.Home;

public class ScanListItem
{
    public long Id { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeLastName { get; set; } = string.Empty;

    public string EmployeeFirstName { get; set; } = string.Empty;

    public string? Mark { get; set; }

    public string Badge { get; set; } = string.Empty;

    public bool InOut { get; set; }

    public DateTime InsertedMoment { get; set; }

    public int? WorkStationId { get; set; }

    public string? WorkStationName { get; set; }

    public List<string> EmployeeOrganizationalUnitNames { get; set; } = new();

    public string? Ip { get; set; }
}
