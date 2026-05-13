using Microsoft.EntityFrameworkCore;
using Pontaj.Database.Pontaj;

namespace Pontaj.Services.Logs;

public interface IAppLogger
{
    Task LogAsync(string action, string message, Exception? ex = null, string? username = null);
}

public class AppLogger : IAppLogger
{
    // Use a DbContext factory so each log write gets its own short-lived context.
    // Sharing the request-scoped DbContext was unsafe in catch blocks: if the original
    // SaveChangesAsync failed mid-flow, the same context still tracked the failed entity,
    // and the next save (the log row) would either retry the bad insert or compound the
    // error. A dedicated context per log call sidesteps that entirely.
    private readonly IDbContextFactory<PontajContext> _contextFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AppLogger(IDbContextFactory<PontajContext> contextFactory, IHttpContextAccessor httpContextAccessor)
    {
        _contextFactory = contextFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string message, Exception? ex = null, string? username = null)
    {
        if (string.IsNullOrEmpty(username))
        {
            var user = _httpContextAccessor.HttpContext?.User;
            username = user?.Identity?.IsAuthenticated == true
                ? user.Identity.Name
                : "Anonymous";
        }

        var exceptionMessage = "";

        if (ex != null)
        {
            exceptionMessage = ex.Message;
            if (ex.InnerException != null)
            {
                exceptionMessage += ex.InnerException.Message;
            }
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var log = new LogEntries
        {
            Username = username ?? "Anonymous",
            Action = action,
            Message = message,
            StackTrace = exceptionMessage,
            LoggedAt = DateTime.Now
        };

        context.LogEntries.Add(log);
        await context.SaveChangesAsync();
    }
}
