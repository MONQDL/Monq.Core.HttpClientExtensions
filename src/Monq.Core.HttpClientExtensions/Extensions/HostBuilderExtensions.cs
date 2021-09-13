using IdentityModel.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Monq.Core.HttpClientExtensions;
using Monq.Core.HttpClientExtensions.Exceptions;
using System;
using static Monq.Core.HttpClientExtensions.AuthConstants;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Методы расширения для сборщика универсального узла приложения (<see cref="IHostBuilder"/>).
    /// </summary>
    public static class HostBuilderExtensions
    {
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

                    var configuration = builderContext.Configuration;
                    RestHttpClient.AuthorizationRequest += async (client) =>
                    {
                        // TODO: Вынести хардкод в опции и Default конфигурацию.
                        // Вынести хардкод в опции и Default конфигурацию.
                        var authConfig = configuration.GetSection(Authentication.AuthenticationSection);

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
                    };
                });

        /// <summary>
        /// Применить конфигурацию обработки http-запроса для <see cref="RestHttpClient"/>.
        /// </summary>
        /// <param name="hostBuilder">Сборщик универсального узла приложения.</param>
        /// <param name="setupAction">Конфигуратор базового http-клиента.</param>
        public static IHostBuilder ConfigBasicHttpService(this IHostBuilder hostBuilder, Action<BasicHttpServiceOptions> setupAction) =>
            hostBuilder
                .ConfigureServices((builderContext, config) =>
                {
                    config.Configure(setupAction);
                    config.AddOptions();
                    config.AddSingleton(resolver => resolver.GetRequiredService<IOptions<BasicHttpServiceOptions>>().Value);
                });
    }
}
