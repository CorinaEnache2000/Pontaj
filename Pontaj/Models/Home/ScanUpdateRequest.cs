namespace Pontaj.Models.Home;

public class ScanUpdateRequest
{
    public long Id { get; set; }

    public bool InOut { get; set; }

    public DateTime Moment { get; set; }

    public int? WorkStationId { get; set; }

    public int? OrganizationalUnitId { get; set; }
}
