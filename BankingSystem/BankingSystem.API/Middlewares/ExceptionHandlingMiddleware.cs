using BankingSystem.Business.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.API.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogInformation("Request {Method} {Path} was cancelled by the client",
                    context.Request.Method, context.Request.Path);
            }
            catch (AppException ex) when (!context.Response.HasStarted)
            {
                if (ex.StatusCode >= 500)
                {
                    _logger.LogError(ex, "Server-side application error {ExceptionType} on {Method} {Path}",
                        ex.GetType().Name, context.Request.Method, context.Request.Path);
                }
                else
                {
                    _logger.LogWarning("Request rejected: {ExceptionType} ({StatusCode}) on {Method} {Path}",
                        ex.GetType().Name, ex.StatusCode, context.Request.Method, context.Request.Path);
                }

                var detail = ex.StatusCode >= 500 ? "A server error occurred. Please try again later." : ex.Message;
                await WriteProblemAsync(context, ex.StatusCode, detail);
            }
            catch (DbUpdateConcurrencyException) when (!context.Response.HasStarted)
            {
                _logger.LogWarning("Concurrency conflict on {Method} {Path}", context.Request.Method, context.Request.Path);
                await WriteProblemAsync(context, StatusCodes.Status409Conflict,
                    "The data was changed by another request. Reload and try again.");
            }
            catch (Exception ex) when (!context.Response.HasStarted)
            {
                _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
                await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred.");
            }
        }

        private static Task WriteProblemAsync(HttpContext context, int statusCode, string detail)
        {
            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = ReasonPhrases.GetReasonPhrase(statusCode),
                Detail = detail,
                Instance = context.Request.Path
            };

            problem.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = statusCode;
            return context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
        }
    }
}