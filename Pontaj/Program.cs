using System.Runtime.Versioning;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pontaj.Database.Pontaj;
using Pontaj.Filters;
using Pontaj.Models;
using Pontaj.Repositories;
using Pontaj.Services.Admin;
using Pontaj.Services.Login;
using Pontaj.Services.Logs;
using Pontaj.Services.Scan;

[assembly: SupportedOSPlatform("windows")]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<JwtRefreshFilter>();
});

// Replace the default [ApiController] ProblemDetails response on model-binding failures
// (malformed JSON, missing required fields, etc.) with our ResponseBase envelope, so the
// 400 shape is consistent with every other JSON response in the app.
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(kv => kv.Value?.Errors.Count > 0)
            .SelectMany(kv => kv.Value!.Errors.Select(e => e.ErrorMessage))
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .ToArray();

        var reason = errors.Length > 0
            ? string.Join(" ", errors)
            : "Cerere invalidă.";

        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(ResponseBase.Error(reason));
    };
});

builder.Services.AddDbContext<PontajContext>();
// Separate factory for AppLogger so log writes (often in catch blocks) don't
// share the request DbContext, which may be in a faulted state after a failed save.
// Lifetime must be Scoped to match the DbContextOptions registered by AddDbContext above
// — a Singleton factory can't consume the Scoped DbContextOptions (DI validator rejects).
// The factory is stateless; per-request allocation is negligible. Each CreateDbContext call
// still produces a fresh isolated context.
builder.Services.AddDbContextFactory<PontajContext>(lifetime: ServiceLifetime.Scoped);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
builder.Services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IWorkStationRepository, WorkStationRepository>();
builder.Services.AddScoped<IPunchRepository, PunchRepository>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.ContentType = "application/json";
        await ctx.HttpContext.Response.WriteAsJsonAsync(
            ResponseBase.Error("Prea multe scanări într-un interval scurt. Reîncercați."),
            cancellationToken: ct);
    };

    // Per-IP fixed window for the anonymous scan endpoint.
    // 60/min comfortably covers shift-change bursts while blocking abuse loops.
    options.AddPolicy("scan", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAppLogger, AppLogger>();

builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmployeeAdminService, EmployeeAdminService>();
builder.Services.AddScoped<IOrganizationalUnitAdminService, OrganizationalUnitAdminService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.AddSingleton<IActiveDirectoryService>(
    new ActiveDirectoryService(ldapServer: "Dc-01.intranet.local", domain: "INTRANET"));

string jwtSigningKey;
TimeSpan jwtLifetime;
int jwtRefreshThresholdPercent;
int scanLockTimeoutMs = ScanSettings.DefaultLockTimeoutMs;
using (var bootstrapContext = new PontajContext())
{
    var configRepo = new ConfigurationRepository(bootstrapContext);

    // Scan lock timeout: best-effort load — fall back to the compile-time default on any
    // failure (row missing, unparseable value, transient DB hiccup). The scan endpoint
    // must remain functional even if this row is absent.
    try
    {
        var scanLockRaw = configRepo.GetValue(ScanSettings.LockTimeoutMsConfigName);
        if (int.TryParse(scanLockRaw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsedScanLock) && parsedScanLock > 0)
        {
            scanLockTimeoutMs = parsedScanLock;
        }
    }
    catch
    {
        // Keep the default; do not block startup.
    }

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
builder.Services.AddSingleton(new ScanRuntimeOptions(scanLockTimeoutMs));

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

                // OnChallenge only fires on protected (JwtCookie) routes, so reaching here
                // always means the user was redirected from a protected page — show the
                // "session expired / please log in" alert in either case. Clear any stale
                // session cookie too so the browser stops sending it.
                if (ctx.Request.Cookies.ContainsKey(AuthSchemes.SessionCookieName))
                {
                    ctx.Response.Cookies.Delete(AuthSchemes.SessionCookieName, new CookieOptions
                    {
                        Path = "/",
                        Secure = true,
                        SameSite = SameSiteMode.Strict
                    });
                }

                ctx.Response.Redirect("/Account/Login?expired=1");
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

app.UseRateLimiter();

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
