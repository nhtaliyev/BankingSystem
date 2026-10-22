using BankingSystem.Business.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.API
{
    public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetails) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
        {
            var (status, title) = ex switch
            {
                AppException app => (app.StatusCode, app.Message),
                DbUpdateConcurrencyException => (409, "This record was changed by someone else. Reload and try again."),
                _ => (500, "An unexpected error occurred.")
            };

            if (status >= 500) logger.LogError(ex, "Unhandled exception");
            else logger.LogWarning("Handled {Type}: {Message}", ex.GetType().Name, ex.Message);

            ctx.Response.StatusCode = status;
            return await problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = ctx,
                Exception = ex,
                ProblemDetails = new() { Status = status, Title = title }
            });
        }
    }
}
