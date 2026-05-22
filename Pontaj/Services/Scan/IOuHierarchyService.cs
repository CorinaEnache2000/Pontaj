namespace Pontaj.Services.Scan;

public interface IOuHierarchyService
{
    Task<HashSet<int>> GetDescendantIdsAsync(IEnumerable<int> rootIds, CancellationToken ct = default);
}
