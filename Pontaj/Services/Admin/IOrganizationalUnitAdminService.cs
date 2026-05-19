using Pontaj.Models.Admin.OrganizationalUnits;

namespace Pontaj.Services.Admin;

public interface IOrganizationalUnitAdminService
{
    Task<OrganizationalUnitsViewModel> GetTreeAsync(CancellationToken ct = default);

    Task<OrganizationalUnitDetail?> GetDetailAsync(int id, CancellationToken ct = default);

    Task<List<OrganizationalUnitWorkStationItem>> GetWorkStationsAsync(int id, CancellationToken ct = default);
}
