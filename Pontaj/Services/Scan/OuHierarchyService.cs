using Microsoft.EntityFrameworkCore;
using Pontaj.Database.Pontaj;

namespace Pontaj.Services.Scan;

public class OuHierarchyService : IOuHierarchyService
{
    private readonly PontajContext _context;

    public OuHierarchyService(PontajContext context)
    {
        _context = context;
    }

    public async Task<HashSet<int>> GetDescendantIdsAsync(IEnumerable<int> rootIds, CancellationToken ct = default)
    {
        var roots = rootIds?.Distinct().ToList() ?? new List<int>();
        var result = new HashSet<int>();
        if (roots.Count == 0)
        {
            return result;
        }

        var all = await _context.OrganizationalUnits
            .Select(o => new { o.Id, o.ParentOrganizationalUnitId, o.Active })
            .ToListAsync(ct);

        var childrenByParent = new Dictionary<int, List<int>>(all.Count);
        foreach (var node in all)
        {
            if (!node.Active)
            {
                continue;
            }
            if (node.ParentOrganizationalUnitId.HasValue && node.ParentOrganizationalUnitId.Value != node.Id)
            {
                if (!childrenByParent.TryGetValue(node.ParentOrganizationalUnitId.Value, out var bucket))
                {
                    bucket = new List<int>();
                    childrenByParent[node.ParentOrganizationalUnitId.Value] = bucket;
                }
                bucket.Add(node.Id);
            }
        }

        var activeIds = all.Where(n => n.Active).Select(n => n.Id).ToHashSet();

        var queue = new Queue<int>();
        foreach (var root in roots)
        {
            if (activeIds.Contains(root) && result.Add(root))
            {
                queue.Enqueue(root);
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!childrenByParent.TryGetValue(current, out var children))
            {
                continue;
            }
            foreach (var child in children)
            {
                if (result.Add(child))
                {
                    queue.Enqueue(child);
                }
            }
        }

        return result;
    }
}
