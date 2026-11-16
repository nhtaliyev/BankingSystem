using BankingSystem.MVC.ViewModels;
using Microsoft.AspNetCore.Authentication;
using System.Globalization;
using System.Net.Http.Headers;

namespace BankingSystem.MVC.Services.Http
{
    public class TokenHandler : DelegatingHandler
    {
        public const string RawClientName = "BankingApi.Raw";

        private const string RefreshedTokenItemKey = "BankingApi.RefreshedAccessToken";
        private static readonly TimeSpan RefreshWindow = TimeSpan.FromSeconds(60);

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<TokenHandler> _logger;

        public TokenHandler(
            IHttpContextAccessor httpContextAccessor,
            IHttpClientFactory clientFactory,
            ILogger<TokenHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _clientFactory = clientFactory;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;

            if (context?.User.Identity?.IsAuthenticated == true)
            {
                var accessToken = await GetValidAccessTokenAsync(context, cancellationToken);
                if (!string.IsNullOrEmpty(accessToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private async Task<string?> GetValidAccessTokenAsync(HttpContext context, CancellationToken cancellationToken)
        {
            if (context.Items[RefreshedTokenItemKey] is string refreshedToken)
                return refreshedToken;

            var accessToken = await context.GetTokenAsync("access_token");
            var refreshToken = await context.GetTokenAsync("refresh_token");
            var expiresRaw = await context.GetTokenAsync("expires_at");

            if (accessToken is null || refreshToken is null)
                return accessToken;

            var hasValidExpiry = DateTimeOffset.TryParse(
                expiresRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiresAt);

            if (hasValidExpiry && expiresAt - DateTimeOffset.UtcNow > RefreshWindow)
                return accessToken;

            return await RefreshAsync(context, accessToken, refreshToken, cancellationToken) ?? accessToken;
        }

        private async Task<string?> RefreshAsync(HttpContext context, string accessToken, string refreshToken, CancellationToken cancellationToken)
        {
            try
            {
                var client = _clientFactory.CreateClient(RawClientName);
                using var response = await client.PostAsJsonAsync(
                    "api/auth/refresh", new RefreshRequest(accessToken, refreshToken), cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Token refresh was rejected with status {StatusCode}", (int)response.StatusCode);
                    return null;
                }

                var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
                var authResult = await context.AuthenticateAsync();
                if (tokens is null || !authResult.Succeeded || authResult.Properties is null || authResult.Principal is null)
                    return null;

                authResult.Properties.UpdateTokenValue("access_token", tokens.AccessToken);
                authResult.Properties.UpdateTokenValue("refresh_token", tokens.RefreshToken);
                authResult.Properties.UpdateTokenValue("expires_at", tokens.ExpireDate.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));

                await context.SignInAsync(authResult.Principal, authResult.Properties);

                context.Items[RefreshedTokenItemKey] = tokens.AccessToken;
                return tokens.AccessToken;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Token refresh failed because the API could not be reached");
                return null;
            }
        }
    }
}
