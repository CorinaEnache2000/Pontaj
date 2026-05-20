namespace Pontaj.Models.Admin.WorkStations;

public class WorkStationsViewModel
{
    public List<WorkStationListItem> WorkStations { get; set; } = new();

    public List<OrganizationalUnitOption> OrganizationalUnits { get; set; } = new();
}
