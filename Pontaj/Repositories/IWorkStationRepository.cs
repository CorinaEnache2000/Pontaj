using Pontaj.Database.Pontaj;

namespace Pontaj.Repositories;

public interface IWorkStationRepository
{
    Task<WorkStations?> GetActiveByIpAsync(string ip, CancellationToken ct = default);
    Task<WorkStations?> GetActiveByHostnameAsync(string hostname, CancellationToken ct = default);
    Task<WorkStations> GetOrCreateByHostnameAsync(string hostname, string? ip, int organizationalUnitId, CancellationToken ct = default);
}
