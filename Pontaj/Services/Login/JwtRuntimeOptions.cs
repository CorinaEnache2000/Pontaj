namespace Pontaj.Services.Login;

public class JwtRuntimeOptions
{
    public string SigningKey { get; }
    public TimeSpan TokenLifetime { get; }
    public int RefreshThresholdPercent { get; }

    public JwtRuntimeOptions(string signingKey, TimeSpan tokenLifetime, int refreshThresholdPercent)
    {
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new ArgumentException("JWT signing key must not be empty.", nameof(signingKey));
        }
        if (tokenLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(tokenLifetime), "JWT token lifetime must be positive.");
        }
        if (refreshThresholdPercent < 0 || refreshThresholdPercent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(refreshThresholdPercent), "JWT refresh threshold percent must be between 0 and 100.");
        }
        SigningKey = signingKey;
        TokenLifetime = tokenLifetime;
        RefreshThresholdPercent = refreshThresholdPercent;
    }
}
