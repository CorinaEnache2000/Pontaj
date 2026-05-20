namespace Pontaj.Models.Admin.WorkStations;

public class CreateWorkStationRequest
{
    public string? Name { get; set; }

    public string? Ip { get; set; }

    public int? OrganizationalUnitId { get; set; }

    public bool Active { get; set; } = true;
}
