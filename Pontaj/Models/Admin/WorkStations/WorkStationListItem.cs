namespace Pontaj.Models.Admin.WorkStations;

public class WorkStationListItem
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Ip { get; set; }

    public int OrganizationalUnitId { get; set; }

    public string OrganizationalUnitName { get; set; } = null!;

    public bool Active { get; set; }
}
