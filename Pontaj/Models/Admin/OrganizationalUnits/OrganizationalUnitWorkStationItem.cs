namespace Pontaj.Models.Admin.OrganizationalUnits;

public class OrganizationalUnitWorkStationItem
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Ip { get; set; }

    public bool Active { get; set; }
}
