using System.Security.Claims;
using Serilog.Context;

namespace BankingSystem.API.Middlewares
{
    public class LogUserContextMiddleware
    {
        private readonly RequestDelegate _next;

        public LogUserContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";

            using (LogContext.PushProperty("UserId", userId))
            {
                await _next(context);
            }
        }
    }
}