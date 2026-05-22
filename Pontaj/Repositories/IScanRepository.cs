using Pontaj.Database.Pontaj;
using Pontaj.Models.Home;
using Pontaj.Services.Scan;

namespace Pontaj.Repositories;

public interface IScanRepository
{
    Task<(List<ScanListItem> Items, int Total)> GetPageAsync(
        ScanPageRequest request,
        ScanScope scope,
        CancellationToken ct = default);

    Task<Punches?> GetByIdAsync(long id, CancellationToken ct = default);

    Task InsertAsync(Punches punch, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);

    Task DeleteAsync(Punches punch, CancellationToken ct = default);
}
