using Pontaj.Database.Pontaj;
using Microsoft.Extensions.Logging.Abstractions;

namespace Pontaj.Services.Logs
{
    public interface IAppLogger
    {
        Task LogAsync(string action, string message, Exception ex = null, string username = null);
    }
    public class AppLogger : IAppLogger
    {
        private readonly PontajContext _pontajContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AppLogger(PontajContext pontajContext, IHttpContextAccessor httpContextAccessor)
        {
            _pontajContext = pontajContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string action, string message, Exception ex = null, string username = null)
        {
            if (string.IsNullOrEmpty(username))
            {
                var user = _httpContextAccessor.HttpContext?.User;
                username = user?.Identity?.IsAuthenticated == true
                    ? user.Identity.Name
                    : "Anonymous";
            }

            var exceptionmessage = "";

            if (ex != null)
            {
                exceptionmessage = ex.Message;
                if (ex.InnerException != null)
                {
                    exceptionmessage += ex.InnerException.Message;
                }
            }

            var log = new LogEntries
            {
                Username = username,
                Action = action,
                Message = message,
                StackTrace = exceptionmessage,
                LoggedAt = DateTime.Now
            };

            _pontajContext.LogEntries.Add(log);
            await _pontajContext.SaveChangesAsync();
        }
    }
}

