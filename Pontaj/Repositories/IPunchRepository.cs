using Pontaj.Database.Pontaj;

namespace Pontaj.Repositories;

public interface IPunchRepository
{
    Task InsertWithDirectionInferenceAsync(Punches punch, CancellationToken ct = default);
}
