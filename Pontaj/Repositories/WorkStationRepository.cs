using Microsoft.EntityFrameworkCore;
using Pontaj.Database.Pontaj;

namespace Pontaj.Repositories;

public class WorkStationRepository : IWorkStationRepository
{
    private readonly PontajContext _context;

    public WorkStationRepository(PontajContext context)
    {
        _context = context;
    }

    public Task<WorkStations?> GetActiveByIpAsync(string ip, CancellationToken ct = default) =>
        _context.WorkStations.FirstOrDefaultAsync(w => w.Active && w.Ip == ip, ct);

    public Task<WorkStations?> GetActiveByHostnameAsync(string hostname, CancellationToken ct = default) =>
        _context.WorkStations.FirstOrDefaultAsync(
            w => w.Active && w.NameKey.ToUpper() == hostname.ToUpper(), ct);

    public async Task<WorkStations> GetOrCreateByHostnameAsync(
        string hostname, string? ip, int organizationalUnitId, CancellationToken ct = default)
    {
        var existing = await GetActiveByHostnameAsync(hostname, ct);
        if (existing != null)
        {
            if (ip != null && existing.Ip != ip)
            {
                existing.Ip = ip;
                await _context.SaveChangesAsync(ct);
            }
            return existing;
        }

        var workStation = new WorkStations
        {
            NameKey = hostname.ToUpperInvariant(),
            Ip = ip,
            Active = true,
            OrganizationalUnitId = organizationalUnitId,
        };
        _context.WorkStations.Add(workStation);
        await _context.SaveChangesAsync(ct);
        return workStation;
    }
}
