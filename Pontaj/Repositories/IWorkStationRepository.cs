using Pontaj.Database.Pontaj;

namespace Pontaj.Repositories;

public interface IWorkStationRepository
{
    Task<WorkStations?> GetActiveByIpAsync(string ip, CancellationToken ct = default);
}
