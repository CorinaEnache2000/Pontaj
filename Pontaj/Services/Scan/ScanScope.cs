namespace Pontaj.Services.Scan;

public sealed class ScanScope
{
    public bool IsAdmin { get; init; }

    public bool IsDirector { get; init; }

    public bool CanEdit => IsAdmin || IsDirector;

    public int? SelfEmployeeId { get; init; }

    public HashSet<int>? AllowedOuIds { get; init; }
}
