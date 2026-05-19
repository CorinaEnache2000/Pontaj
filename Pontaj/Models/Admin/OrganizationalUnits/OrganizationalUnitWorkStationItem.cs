namespace Pontaj.Models.Admin.OrganizationalUnits;

// One row in the "Stații de lucru" tab for a selected organizational unit.
public class OrganizationalUnitWorkStationItem
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Ip { get; set; }

    public bool Active { get; set; }
}
