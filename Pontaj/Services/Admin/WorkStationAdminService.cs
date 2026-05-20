using Microsoft.EntityFrameworkCore;
using Pontaj.Database.Pontaj;
using Pontaj.Models.Admin.WorkStations;

namespace Pontaj.Services.Admin;

public class WorkStationAdminService : IWorkStationAdminService
{
    private readonly PontajContext _context;

    public WorkStationAdminService(PontajContext context)
    {
        _context = context;
    }

    public async Task<WorkStationsViewModel> GetViewModelAsync(CancellationToken ct = default)
    {
        var workStations = await _context.WorkStations
            .Select(w => new WorkStationListItem
            {
                Id = w.Id,
                Name = w.NameKey,
                Ip = w.Ip,
                OrganizationalUnitId = w.OrganizationalUnitId,
                OrganizationalUnitName = _context.TextResources
                    .Where(tr => tr.ResourceKey == w.OrganizationalUnit.PublicNameKey
                              && tr.LanguageId == w.OrganizationalUnit.DefaultLanguageId)
                    .Select(tr => tr.Value)
                    .FirstOrDefault() ?? w.OrganizationalUnit.PublicNameKey,
                Active = w.Active
            })
            .ToListAsync(ct);

        workStations.Sort(
            (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

        var ouOptions = await GetOrganizationalUnitOptionsAsync(ct);

        return new WorkStationsViewModel
        {
            WorkStations = workStations,
            OrganizationalUnits = ouOptions
        };
    }

    public async Task<WorkStationDetail?> GetDetailAsync(int id, CancellationToken ct = default)
    {
        return await _context.WorkStations
            .Where(w => w.Id == id)
            .Select(w => new WorkStationDetail
            {
                Id = w.Id,
                Name = w.NameKey,
                Ip = w.Ip,
                OrganizationalUnitId = w.OrganizationalUnitId,
                OrganizationalUnitName = _context.TextResources
                    .Where(tr => tr.ResourceKey == w.OrganizationalUnit.PublicNameKey
                              && tr.LanguageId == w.OrganizationalUnit.DefaultLanguageId)
                    .Select(tr => tr.Value)
                    .FirstOrDefault() ?? w.OrganizationalUnit.PublicNameKey,
                Active = w.Active
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<(string? ValidationError, int? CreatedId)> CreateAsync(
        CreateWorkStationRequest request, CancellationToken ct = default)
    {
        var name = request.Name?.Trim();
        var ip = string.IsNullOrWhiteSpace(request.Ip) ? null : request.Ip.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return ("Numele stației este obligatoriu.", null);
        }

        if (request.OrganizationalUnitId == null)
        {
            return ("Unitatea organizațională este obligatorie.", null);
        }

        var ouExists = await _context.OrganizationalUnits
            .AnyAsync(o => o.Id == request.OrganizationalUnitId.Value, ct);
        if (!ouExists)
        {
            return ("Unitatea organizațională selectată nu există.", null);
        }

        var nameTaken = await _context.WorkStations
            .AnyAsync(w => w.NameKey == name, ct);
        if (nameTaken)
        {
            return ("Există deja o stație cu acest nume.", null);
        }

        if (ip != null)
        {
            var ipTaken = await _context.WorkStations.AnyAsync(w => w.Ip == ip, ct);
            if (ipTaken)
            {
                return ("Există deja o stație cu acest IP.", null);
            }
        }

        var entity = new WorkStations
        {
            NameKey = name,
            Ip = ip,
            OrganizationalUnitId = request.OrganizationalUnitId.Value,
            Active = request.Active
        };

        _context.WorkStations.Add(entity);
        await _context.SaveChangesAsync(ct);

        return (null, entity.Id);
    }

    public async Task<string?> UpdateAsync(UpdateWorkStationRequest request, CancellationToken ct = default)
    {
        var name = request.Name?.Trim();
        var ip = string.IsNullOrWhiteSpace(request.Ip) ? null : request.Ip.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return "Numele stației este obligatoriu.";
        }
        if (request.OrganizationalUnitId == null)
        {
            return "Unitatea organizațională este obligatorie.";
        }

        var entity = await _context.WorkStations.FirstOrDefaultAsync(w => w.Id == request.Id, ct);
        if (entity == null)
        {
            return "Stația de lucru nu există.";
        }

        var ouExists = await _context.OrganizationalUnits
            .AnyAsync(o => o.Id == request.OrganizationalUnitId.Value, ct);
        if (!ouExists)
        {
            return "Unitatea organizațională selectată nu există.";
        }

        var nameTaken = await _context.WorkStations
            .AnyAsync(w => w.Id != request.Id && w.NameKey == name, ct);
        if (nameTaken)
        {
            return "Există deja o stație cu acest nume.";
        }

        if (ip != null)
        {
            var ipTaken = await _context.WorkStations
                .AnyAsync(w => w.Id != request.Id && w.Ip == ip, ct);
            if (ipTaken)
            {
                return "Există deja o stație cu acest IP.";
            }
        }

        entity.NameKey = name;
        entity.Ip = ip;
        entity.OrganizationalUnitId = request.OrganizationalUnitId.Value;
        entity.Active = request.Active;

        await _context.SaveChangesAsync(ct);
        return null;
    }

    public async Task<string?> SetActiveAsync(SetWorkStationActiveRequest request, CancellationToken ct = default)
    {
        var entity = await _context.WorkStations.FirstOrDefaultAsync(w => w.Id == request.Id, ct);
        if (entity == null)
        {
            return "Stația de lucru nu există.";
        }

        entity.Active = request.Active;
        await _context.SaveChangesAsync(ct);
        return null;
    }

    private async Task<List<OrganizationalUnitOption>> GetOrganizationalUnitOptionsAsync(CancellationToken ct)
    {
        var ous = await _context.OrganizationalUnits
            .Select(o => new OrganizationalUnitOption
            {
                Id = o.Id,
                Name = _context.TextResources
                    .Where(tr => tr.ResourceKey == o.PublicNameKey
                              && tr.LanguageId == o.DefaultLanguageId)
                    .Select(tr => tr.Value)
                    .FirstOrDefault() ?? o.PublicNameKey
            })
            .ToListAsync(ct);

        ous.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return ous;
    }
}
