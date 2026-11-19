using BankingSystem.MVC.Options;
using BankingSystem.MVC.Services.Http;
using Microsoft.Extensions.Options;

namespace BankingSystem.MVC.Extensions
{
    public static class ApiClientRegistration
    {
        public static void ConfigureClient(IServiceProvider services, HttpClient client)
        {
            var options = services.GetRequiredService<IOptions<ApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(30);
        }

        public static IHttpClientBuilder AddApiClient<TClient, TImplementation>(this IServiceCollection services)
            where TClient : class
            where TImplementation : class, TClient =>
            services
                .AddHttpClient<TClient, TImplementation>(ConfigureClient)
                .AddHttpMessageHandler<TokenHandler>();
    }
}
