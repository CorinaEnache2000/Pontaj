using Microsoft.EntityFrameworkCore;
using Pontaj.Database.Pontaj;
using Pontaj.Models.Admin.OrganizationalUnits;

namespace Pontaj.Services.Admin;

public class OrganizationalUnitAdminService : IOrganizationalUnitAdminService
{
    private readonly PontajContext _context;

    public OrganizationalUnitAdminService(PontajContext context)
    {
        _context = context;
    }

    public async Task<OrganizationalUnitsViewModel> GetTreeAsync(CancellationToken ct = default)
    {
        var flat = await _context.OrganizationalUnits
            .Select(ou => new OrganizationalUnitTreeNode
            {
                Id = ou.Id,
                ParentId = ou.ParentOrganizationalUnitId,
                Active = ou.Active,
                Name = _context.TextResources
                    .Where(tr => tr.ResourceKey == ou.PublicNameKey
                              && tr.LanguageId == ou.DefaultLanguageId)
                    .Select(tr => tr.Value)
                    .FirstOrDefault() ?? ou.PublicNameKey
            })
            .ToListAsync(ct);

        var byId = flat.ToDictionary(n => n.Id);
        var roots = new List<OrganizationalUnitTreeNode>();

        foreach (var node in flat)
        {
            if (node.ParentId.HasValue
                && node.ParentId.Value != node.Id
                && byId.TryGetValue(node.ParentId.Value, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        SortRecursive(roots);
        return new OrganizationalUnitsViewModel { Roots = roots };
    }

    public async Task<OrganizationalUnitDetail?> GetDetailAsync(int id, CancellationToken ct = default)
    {
        var raw = await _context.OrganizationalUnits
            .Where(o => o.Id == id)
            .Select(o => new
            {
                o.Id,
                Denumire = _context.TextResources
                    .Where(tr => tr.ResourceKey == o.InternalNameKey
                              && tr.LanguageId == o.DefaultLanguageId)
                    .Select(tr => tr.Value)
                    .FirstOrDefault() ?? o.InternalNameKey,
                DenumireCompleta = _context.TextResources
                    .Where(tr => tr.ResourceKey == o.PublicNameKey
                              && tr.LanguageId == o.DefaultLanguageId)
                    .Select(tr => tr.Value)
                    .FirstOrDefault() ?? o.PublicNameKey,
                TypeName = _context.TextResources
                    .Where(tr => tr.ResourceKey == o.OrganizationalUnitType.NameKey
                              && tr.LanguageId == o.DefaultLanguageId)
                    .Select(tr => tr.Value)
                    .FirstOrDefault() ?? o.OrganizationalUnitType.NameKey,
                ParentName = o.ParentOrganizationalUnit == null
                    ? null
                    : (_context.TextResources
                        .Where(tr => tr.ResourceKey == o.ParentOrganizationalUnit.PublicNameKey
                                  && tr.LanguageId == o.ParentOrganizationalUnit.DefaultLanguageId)
                        .Select(tr => tr.Value)
                        .FirstOrDefault() ?? o.ParentOrganizationalUnit.PublicNameKey),
                LanguageLabel = o.DefaultLanguage.Label,
                o.Active,
                StreetAbbr = o.Address != null && o.Address.Street != null
                    ? o.Address.Street.StreetType.Abbreviation : null,
                StreetName = o.Address != null && o.Address.Street != null
                    ? o.Address.Street.Name : null,
                LocalityName = o.Address != null && o.Address.Street != null
                    ? o.Address.Street.Locality.Name : null,
                Number = o.Address != null ? o.Address.Number : null,
                Building = o.Address != null ? o.Address.Building : null,
                Entrance = o.Address != null ? o.Address.Entrance : null,
                Floor = o.Address != null ? o.Address.Floor : null,
                Apartment = o.Address != null ? o.Address.Apartment : null,
                Line1 = o.Address != null ? o.Address.Line1 : null,
                Line2 = o.Address != null ? o.Address.Line2 : null
            })
            .FirstOrDefaultAsync(ct);

        if (raw == null)
        {
            return null;
        }

        return new OrganizationalUnitDetail
        {
            Id = raw.Id,
            Denumire = raw.Denumire,
            DenumireCompleta = raw.DenumireCompleta,
            TypeName = raw.TypeName,
            ParentName = raw.ParentName,
            LanguageLabel = raw.LanguageLabel,
            Active = raw.Active,
            Address = ComposeAddress(
                raw.StreetAbbr, raw.StreetName, raw.Number, raw.Building,
                raw.Entrance, raw.Floor, raw.Apartment, raw.LocalityName,
                raw.Line1, raw.Line2)
        };
    }

    private static string? ComposeAddress(
        string? streetAbbr, string? streetName, string? number, string? building,
        string? entrance, string? floor, string? apartment, string? localityName,
        string? line1, string? line2)
    {
        var parts = new List<string>();

        var street = string.Join(" ", new[] { streetAbbr, streetName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        if (!string.IsNullOrWhiteSpace(street))
        {
            parts.Add(street);
        }
        else if (!string.IsNullOrWhiteSpace(line1))
        {
            parts.Add(line1!.Trim());
            if (!string.IsNullOrWhiteSpace(line2))
            {
                parts.Add(line2!.Trim());
            }
        }

        if (!string.IsNullOrWhiteSpace(number))
        {
            parts.Add($"nr. {number!.Trim()}");
        }
        if (!string.IsNullOrWhiteSpace(building))
        {
            parts.Add($"bl. {building!.Trim()}");
        }
        if (!string.IsNullOrWhiteSpace(entrance))
        {
            parts.Add($"sc. {entrance!.Trim()}");
        }
        if (!string.IsNullOrWhiteSpace(floor))
        {
            parts.Add($"et. {floor!.Trim()}");
        }
        if (!string.IsNullOrWhiteSpace(apartment))
        {
            parts.Add($"ap. {apartment!.Trim()}");
        }
        if (!string.IsNullOrWhiteSpace(localityName))
        {
            parts.Add(localityName!.Trim());
        }

        var result = string.Join(", ", parts);
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    public async Task<string?> SetActiveAsync(int id, bool active, CancellationToken ct = default)
    {
        var entity = await _context.OrganizationalUnits.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (entity == null)
        {
            return "Unitatea organizațională nu există.";
        }
        entity.Active = active;
        await _context.SaveChangesAsync(ct);
        return null;
    }

    public async Task<List<OrganizationalUnitWorkStationItem>> GetWorkStationsAsync(int id, CancellationToken ct = default)
    {
        return await _context.WorkStations
            .Where(w => w.OrganizationalUnitId == id)
            .OrderBy(w => w.NameKey)
            .Select(w => new OrganizationalUnitWorkStationItem
            {
                Id = w.Id,
                Name = w.NameKey,
                Ip = w.Ip,
                Active = w.Active
            })
            .ToListAsync(ct);
    }

    private static void SortRecursive(List<OrganizationalUnitTreeNode> nodes)
    {
        nodes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        foreach (var node in nodes)
        {
            SortRecursive(node.Children);
        }
    }
}
