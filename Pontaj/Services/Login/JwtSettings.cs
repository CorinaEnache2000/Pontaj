namespace Pontaj.Services.Login;

public static class JwtSettings
{
    public const string Issuer = "Pontaj";
    public const string Audience = "Pontaj";
    public const string SigningKeyConfigName = "Jwt:SigningKey";
    public const string TokenLifetimeSecondsConfigName = "Jwt:TokenLifetimeSeconds";
    public const string RefreshThresholdPercentConfigName = "Jwt:RefreshThresholdPercent";
}
