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
builder.Services.AddDbContextFactory<PontajContext>(lifetime: ServiceLifetime.Scoped);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
builder.Services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IWorkStationRepository, WorkStationRepository>();
builder.Services.AddScoped<IPunchRepository, PunchRepository>();
builder.Services.AddScoped<IScanRepository, ScanRepository>();

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
builder.Services.AddScoped<IWorkStationAdminService, WorkStationAdminService>();
builder.Services.AddScoped<IOuHierarchyService, OuHierarchyService>();
builder.Services.AddScoped<IScanService, ScanService>();
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
        options.TokenValidationParameters = tokenValidationParameters;
    })
    .AddJwtBearer(AuthSchemes.JwtCookie, options =>
    {
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

app.MapStaticAssets().AllowAnonymous();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
