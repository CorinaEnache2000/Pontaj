using System.Security.Claims;
using Pontaj.Database.Pontaj;
using Pontaj.Models.Home;

namespace Pontaj.Services.Scan;

public interface IScanService
{
    Task<ScanScope> ResolveScopeAsync(ClaimsPrincipal user, CancellationToken ct = default);

    Task<ScansIndexViewModel> BuildIndexViewModelAsync(ScanScope scope, CancellationToken ct = default);

    Task<(List<ScanListItem> Items, int Total)> GetPageAsync(
        ScanPageRequest request,
        ScanScope scope,
        CancellationToken ct = default);

    Task<(string? ValidationError, Punches? Created)> CreateAsync(
        ScanCreateRequest request,
        ScanScope scope,
        string actorUsername,
        CancellationToken ct = default);

    Task<(string? ValidationError, Punches? Updated)> UpdateAsync(
        ScanUpdateRequest request,
        ScanScope scope,
        string actorUsername,
        CancellationToken ct = default);

    Task<string?> DeleteAsync(
        long id,
        ScanScope scope,
        string actorUsername,
        CancellationToken ct = default);
}
