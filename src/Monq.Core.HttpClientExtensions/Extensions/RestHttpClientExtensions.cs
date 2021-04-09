using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Monq.Core.HttpClientExtensions.Exceptions;
using System.Net.Http;
using System.Threading.Tasks;
using static Monq.Core.HttpClientExtensions.AuthConstants;

namespace Monq.Core.HttpClientExtensions.Extensions
{
    /// <summary>
    /// Базовые методы расширения для http-клиента.
    /// </summary>
    public static class RestHttpClientExtensions
    {
        static IConfiguration _configuration;
        const string WriteScope = "write";

        /// <summary>
        /// Выполнить конфигурацию статического метода аутентификации, который будет выполнен при первом запросе HttpClient.
        /// </summary>
        /// <param name="hostBuilder">The host builder.</param>
        public static IHostBuilder ConfigureStaticAuthentication(this IHostBuilder hostBuilder) =>
            hostBuilder
                .ConfigureServices((builderContext, config) =>
                {
                    var authEndpoint = $"{Authentication.AuthenticationSection}:{Authentication.AuthenticationEndpoint}";
                    if (string.IsNullOrEmpty(builderContext.Configuration[authEndpoint]))
                        throw new MissingConfigurationException("Не найдена конфигурация { \"Authentication\": {...} } в загруженных провайдерах конфигураций.");

                    _configuration = builderContext.Configuration;
                    RestHttpClient.AuthorizationRequest += RestHttpClientAuthorizationRequest;
                });

        static async Task<TokenResponse> RestHttpClientAuthorizationRequest(HttpClient client)
        {
            // TODO: Вынести хардкод в опции и Default конфигурацию.
            // Вынести хардкод в опции и Default конфигурацию.
            var authConfig = _configuration.GetSection(Authentication.AuthenticationSection);

            if (!bool.TryParse(authConfig[Authentication.RequireHttpsMetadata], out var requireHttps))
                requireHttps = false;

            var discoveryDocumentRequest = new DiscoveryDocumentRequest
            {
                Address = authConfig[Authentication.AuthenticationEndpoint],
                Policy = new DiscoveryPolicy { RequireHttps = requireHttps }
            };
            var disco = await client.GetDiscoveryDocumentAsync(discoveryDocumentRequest);
            if (disco.IsError) throw new DiscoveryEndpointException(disco.Error, disco.Exception);

            var request = new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = authConfig[Authentication.ClientName],
                ClientSecret = authConfig[Authentication.ClientSecret],
                Scope = WriteScope
            };

            var response = await client.RequestClientCredentialsTokenAsync(request);
            return response;
        }
    }
}
