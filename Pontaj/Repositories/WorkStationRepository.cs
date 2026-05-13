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
}
