using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BankingSystem.MVC.Services.Http;

public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ApiException(
        HttpStatusCode statusCode,
        string message,
        Exception? innerException = null,
        IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
    public bool IsForbidden => StatusCode == HttpStatusCode.Forbidden;
    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;
    public bool IsConflict => StatusCode == HttpStatusCode.Conflict;

    public bool IsBusinessRuleViolation =>
        StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity;

    internal static async Task<ApiException> FromResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ApiProblem? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ApiProblem>(cancellationToken);
        }
        catch (JsonException) { }          
        catch (NotSupportedException) { }  

        var message = (int)response.StatusCode >= 500
            ? "Something went wrong on our side. Please try again later."
            : response.StatusCode == HttpStatusCode.TooManyRequests
                ? problem?.Detail ?? "Too many requests. Please wait a moment and try again."
                : problem?.Detail ?? problem?.Title ?? "The request could not be completed.";

        return new ApiException(response.StatusCode, message, errors: problem?.Errors);
    }
}

internal sealed record ApiProblem(string? Title, string? Detail, Dictionary<string, string[]>? Errors);