using Pontaj.Models.Admin.WorkStations;

namespace Pontaj.Services.Admin;

public interface IWorkStationAdminService
{
    Task<WorkStationsViewModel> GetViewModelAsync(CancellationToken ct = default);

    Task<WorkStationDetail?> GetDetailAsync(int id, CancellationToken ct = default);

    Task<(string? ValidationError, int? CreatedId)> CreateAsync(CreateWorkStationRequest request, CancellationToken ct = default);

    Task<string?> UpdateAsync(UpdateWorkStationRequest request, CancellationToken ct = default);

    Task<string?> SetActiveAsync(SetWorkStationActiveRequest request, CancellationToken ct = default);
}
