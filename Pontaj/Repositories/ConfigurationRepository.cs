using Microsoft.EntityFrameworkCore;
using Pontaj.Database.Pontaj;

namespace Pontaj.Repositories;

public class ConfigurationRepository : IConfigurationRepository
{
    private readonly PontajContext _context;

    public ConfigurationRepository(PontajContext context)
    {
        _context = context;
    }

    public Task<string?> GetValueAsync(string key, CancellationToken ct = default) =>
        _context.Configurations
            .Where(c => c.ConfigKey == key)
            .Select(c => c.ConfigValue)
            .FirstOrDefaultAsync(ct);

    public string? GetValue(string key) =>
        _context.Configurations
            .Where(c => c.ConfigKey == key)
            .Select(c => c.ConfigValue)
            .FirstOrDefault();
}
