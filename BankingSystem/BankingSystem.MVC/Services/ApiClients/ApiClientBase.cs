using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BankingSystem.MVC.Services.Http;

namespace BankingSystem.MVC.Services.ApiClients;

public abstract class ApiClientBase
{
    protected ApiClientBase(HttpClient http) => Http = http;

    protected HttpClient Http { get; }

    protected async Task SendAsync(HttpMethod method, string url, object? body = null, CancellationToken cancellationToken = default)
    {
        using var response = await SendCoreAsync(method, url, body, cancellationToken);
    }

    protected async Task<T> SendAsync<T>(HttpMethod method, string url, object? body = null, CancellationToken cancellationToken = default)
    {
        using var response = await SendCoreAsync(method, url, body, cancellationToken);
        try
        {
            var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            return result ?? throw new ApiException(HttpStatusCode.BadGateway, "The banking service returned an empty response.");
        }
        catch (JsonException ex)
        {
            throw new ApiException(HttpStatusCode.BadGateway, "The banking service returned an unreadable response.", ex);
        }
    }

    private async Task<HttpResponseMessage> SendCoreAsync(HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent.Create(body);

        HttpResponseMessage response;
        try
        {
            response = await Http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(HttpStatusCode.ServiceUnavailable, "The banking service is currently unavailable. Please try again later.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiException(HttpStatusCode.GatewayTimeout, "The banking service took too long to respond.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var exception = await ApiException.FromResponseAsync(response, cancellationToken);
            response.Dispose();
            throw exception;
        }

        return response;
    }
}