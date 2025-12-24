using IdentityModel.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Monq.Core.HttpClientExtensions;
using Monq.Core.HttpClientExtensions.Exceptions;
using System;
using static Monq.Core.HttpClientExtensions.AuthConstants;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for the generic app host builder (<see cref = "IHostBuilder" />).
    /// </summary>
    public static class HostBuilderExtensions
    {
        const string WriteScope = "write";

        /// <summary>
        /// Configure a static authentication method that will be executed on the first HttpClient request.
        /// </summary>
        /// <param name="hostBuilder">The host builder.</param>
        public static IHostBuilder ConfigureStaticAuthentication(this IHostBuilder hostBuilder) =>
            hostBuilder
                .ConfigureServices((builderContext, config) =>
                {
                    var authEndpoint = $"{Authentication.AuthenticationSection}:{Authentication.AuthenticationEndpoint}";
                    if (string.IsNullOrEmpty(builderContext.Configuration[authEndpoint]))
                        throw new MissingConfigurationException("No configuration found { \"Authentication \": {...}} in loaded configuration providers.");

                    var configuration = builderContext.Configuration;
                    RestHttpClient.AuthorizationRequest += async (client) =>
                    {
                        // TODO: Take out the hardcode in options and Default configuration.
                        var authConfig = configuration.GetSection(Authentication.AuthenticationSection);

                        if (!bool.TryParse(authConfig[Authentication.RequireHttpsMetadata], out var requireHttps))
                            requireHttps = false;

                        var discoveryDocumentRequest = new DiscoveryDocumentRequest
                        {
                            Address = authConfig[Authentication.AuthenticationEndpoint],
                            Policy = new DiscoveryPolicy { RequireHttps = requireHttps }
                        };
                        var disco = await client.GetDiscoveryDocumentAsync(discoveryDocumentRequest);
                        if (disco.IsError) 
                            throw new DiscoveryEndpointException(disco.Error, disco.Exception);

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
        /// Apply http request processing configuration for <see cref = "RestHttpClient" />.
        /// </summary>
        /// <param name="hostBuilder">Generic Application Host Builder.</param>
        /// <param name="setupAction">Basic http client configurator.</param>
        public static IHostBuilder ConfigBasicHttpService(this IHostBuilder hostBuilder, Action<RestHttpClientOptions> setupAction) =>
            hostBuilder
                .ConfigureServices((builderContext, config) =>
                {
                    config.Configure(setupAction);
                    config.AddOptions();
                    config.AddSingleton(resolver => resolver.GetRequiredService<IOptions<RestHttpClientOptions>>().Value);
                });

        /// <summary>
        /// Apply http request processing configuration for <see cref = "RestHttpClient" />.
        /// </summary>
        /// <param name="hostBuilder">Generic Application Host Builder.</param>
        public static IHostBuilder ConfigBasicHttpService(this IHostBuilder hostBuilder) =>
            hostBuilder
                .ConfigureServices((builderContext, config) =>
                {
                    config.AddOptions();
                    config.AddSingleton(resolver => new RestHttpClientOptions());
                });
    }
}
