using System.Runtime.Versioning;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pontaj.Database.Pontaj;
using Pontaj.Filters;
using Pontaj.Repositories;
using Pontaj.Services.Login;
using Pontaj.Services.Logs;

[assembly: SupportedOSPlatform("windows")]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<JwtRefreshFilter>();
});

builder.Services.AddDbContext<PontajContext>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
builder.Services.AddScoped<IUserXUserRoleRepository, UserXUserRoleRepository>();
builder.Services.AddScoped<IConfigurationRepository, ConfigurationRepository>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAppLogger, AppLogger>();

builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.AddSingleton<IActiveDirectoryService>(
    new ActiveDirectoryService(ldapServer: "Dc-01.intranet.local", domain: "INTRANET"));

string jwtSigningKey;
TimeSpan jwtLifetime;
int jwtRefreshThresholdPercent;
using (var bootstrapContext = new PontajContext())
{
    var configRepo = new ConfigurationRepository(bootstrapContext);

    jwtSigningKey = configRepo.GetValue(JwtSettings.SigningKeyConfigName)
        ?? throw new InvalidOperationException(
            $"Configuration row '{JwtSettings.SigningKeyConfigName}' is missing from the Pontaj database.");

    var lifetimeRaw = configRepo.GetValue(JwtSettings.TokenLifetimeSecondsConfigName)
        ?? throw new InvalidOperationException(
            $"Configuration row '{JwtSettings.TokenLifetimeSecondsConfigName}' is missing from the Pontaj database.");

    if (!int.TryParse(lifetimeRaw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var lifetimeSeconds) || lifetimeSeconds <= 0)
    {
        throw new InvalidOperationException(
            $"Configuration value for '{JwtSettings.TokenLifetimeSecondsConfigName}' must be a positive integer; got '{lifetimeRaw}'.");
    }

    jwtLifetime = TimeSpan.FromSeconds(lifetimeSeconds);

    var thresholdRaw = configRepo.GetValue(JwtSettings.RefreshThresholdPercentConfigName)
        ?? throw new InvalidOperationException(
            $"Configuration row '{JwtSettings.RefreshThresholdPercentConfigName}' is missing from the Pontaj database.");

    if (!int.TryParse(thresholdRaw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out jwtRefreshThresholdPercent) || jwtRefreshThresholdPercent < 0 || jwtRefreshThresholdPercent > 100)
    {
        throw new InvalidOperationException(
            $"Configuration value for '{JwtSettings.RefreshThresholdPercentConfigName}' must be an integer between 0 and 100; got '{thresholdRaw}'.");
    }
}
builder.Services.AddSingleton(new JwtRuntimeOptions(jwtSigningKey, jwtLifetime, jwtRefreshThresholdPercent));

var tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = JwtSettings.Issuer,
    ValidAudience = JwtSettings.Audience,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
    ClockSkew = TimeSpan.FromMinutes(1)
};

builder.Services
    .AddAuthentication(AuthSchemes.JwtHeader)
    .AddJwtBearer(AuthSchemes.JwtHeader, options =>
    {
        // Reads the token from the Authorization: Bearer header (default behavior).
        options.TokenValidationParameters = tokenValidationParameters;
    })
    .AddJwtBearer(AuthSchemes.JwtCookie, options =>
    {
        // Reads the token from a cookie (set client-side by JS) and redirects to the
        // login page on auth failure instead of returning 401. Used for HTML routes.
        options.TokenValidationParameters = tokenValidationParameters;
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                if (ctx.Request.Cookies.TryGetValue(AuthSchemes.SessionCookieName, out var token))
                {
                    ctx.Token = token;
                }
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.Redirect("/Account/Login");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Static assets must be reachable without auth — the login page itself needs CSS/JS.
app.MapStaticAssets().AllowAnonymous();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
