namespace Pontaj.Models.Admin.WorkStations;

public class UpdateWorkStationRequest
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Ip { get; set; }

    public int? OrganizationalUnitId { get; set; }

    public bool Active { get; set; }
}
