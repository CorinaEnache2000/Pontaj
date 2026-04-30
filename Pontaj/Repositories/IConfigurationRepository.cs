namespace Pontaj.Repositories;

public interface IConfigurationRepository
{
    Task<string?> GetValueAsync(string key, CancellationToken ct = default);
    string? GetValue(string key);
}
