namespace Pontaj.Models.Home;

public class ScanPageRequest
{
    public List<int>? EmployeeIds { get; set; }

    public List<int>? WorkStationIds { get; set; }

    public List<int>? OrganizationalUnitIds { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;
}
