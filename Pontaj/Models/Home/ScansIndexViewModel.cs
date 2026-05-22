namespace Pontaj.Models.Home;

public class ScansIndexViewModel
{
    public bool IsLinked { get; set; }

    public bool CanEdit { get; set; }

    public bool ShowOtherEmployeeFilters { get; set; }

    public bool ShowIpColumn { get; set; }

    public List<EmployeeOption> Employees { get; set; } = new();

    public List<WorkStationOption> WorkStations { get; set; } = new();

    public List<OrganizationalUnitOption> OrganizationalUnits { get; set; } = new();

    public List<OuTreeNode> OrganizationalUnitTree { get; set; } = new();
}
