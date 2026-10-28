using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Monq.Core.HttpClientExtensions;
using Monq.Core.HttpClientExtensions.Exceptions;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static Monq.Core.HttpClientExtensions.AuthConstants;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for the generic app host builder (<see cref = "IHostBuilder" />).
    /// </summary>
    public static class HostBuilderExtensions
    {
        const string AuthenticationHttpClientName = "Monq.Core.HttpClientExtensions.StaticAuthentication";
        const string WriteScope = "write";
        static readonly TimeSpan DefaultAuthenticationTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Configure a static authentication method that will be executed on the first HttpClient request.
        /// Repeated calls do not register additional authentication hosted services.
        /// </summary>
        /// <param name="hostBuilder">The host builder.</param>
        public static IHostBuilder ConfigureStaticAuthentication(this IHostBuilder hostBuilder) =>
            ConfigureStaticAuthentication(hostBuilder, GetConfiguredAuthenticationEndpoint);

        /// <summary>
        /// Configure a static authentication method using the endpoint from configuration
        /// and custom settings for the isolated authentication HTTP client.
        /// </summary>
        /// <param name="hostBuilder">The host builder.</param>
        /// <param name="configureAuthenticationClient">
        /// Configuration for the isolated authentication HTTP client.
        /// </param>
        public static IHostBuilder ConfigureStaticAuthentication(
            this IHostBuilder hostBuilder,
            Action<IHttpClientBuilder> configureAuthenticationClient)
        {
            ArgumentNullException.ThrowIfNull(configureAuthenticationClient);
            return ConfigureStaticAuthentication(
                hostBuilder,
                GetConfiguredAuthenticationEndpoint,
                configureAuthenticationClient);
        }

        /// <summary>
        /// Configure a static authentication method using an explicitly resolved authentication endpoint.
        /// </summary>
        /// <param name="hostBuilder">The host builder.</param>
        /// <param name="authenticationEndpointFactory">
        /// Resolves the absolute authentication endpoint using the final host context.
        /// </param>
        /// <param name="configureAuthenticationClient">
        /// Optional configuration for the isolated authentication HTTP client.
        /// </param>
        public static IHostBuilder ConfigureStaticAuthentication(
            this IHostBuilder hostBuilder,
            Func<HostBuilderContext, Uri> authenticationEndpointFactory,
            Action<IHttpClientBuilder>? configureAuthenticationClient = null)
        {
            ArgumentNullException.ThrowIfNull(hostBuilder);
            ArgumentNullException.ThrowIfNull(authenticationEndpointFactory);

            return hostBuilder.ConfigureServices((builderContext, services) =>
            {
                var authenticationEndpoint = authenticationEndpointFactory(builderContext);
                ArgumentNullException.ThrowIfNull(authenticationEndpoint);
                if (!authenticationEndpoint.IsAbsoluteUri)
                    throw new ArgumentException(
                        "The authentication endpoint must be an absolute URI.",
                        nameof(authenticationEndpointFactory));

                var clientBuilder = services.AddHttpClient(
                    AuthenticationHttpClientName,
                    client => client.Timeout = DefaultAuthenticationTimeout);
                configureAuthenticationClient?.Invoke(clientBuilder);
                services.TryAddSingleton(new StaticAuthenticationEndpoint(authenticationEndpoint));
                services.TryAddEnumerable(
                    ServiceDescriptor.Singleton<IHostedService, StaticAuthenticationService>());
            });
        }

        static Uri GetConfiguredAuthenticationEndpoint(HostBuilderContext context)
        {
            var configurationKey =
                $"{Authentication.AuthenticationSection}:{Authentication.AuthenticationEndpoint}";
            var value = context.Configuration[configurationKey];
            if (string.IsNullOrWhiteSpace(value))
                throw new MissingConfigurationException(
                    "No configuration found { \"Authentication\": {...}} in loaded configuration providers.");

            if (!Uri.TryCreate(value, UriKind.Absolute, out var endpoint))
                throw new MissingConfigurationException(
                    $"Configuration value '{configurationKey}' must be an absolute URI.");

            return endpoint;
        }

        sealed record StaticAuthenticationEndpoint(Uri Value);

        sealed class StaticAuthenticationService : IHostedService
        {
            readonly IConfiguration _configuration;
            readonly IHttpClientFactory _httpClientFactory;
            readonly StaticAuthenticationEndpoint _authenticationEndpoint;

            public StaticAuthenticationService(
                IConfiguration configuration,
                IHttpClientFactory httpClientFactory,
                StaticAuthenticationEndpoint authenticationEndpoint)
            {
                _configuration = configuration;
                _httpClientFactory = httpClientFactory;
                _authenticationEndpoint = authenticationEndpoint;
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                RestHttpClient.AuthorizationRequest += RequestTokenAsync;
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                RestHttpClient.AuthorizationRequest -= RequestTokenAsync;
                return Task.CompletedTask;
            }

            async Task<TokenResponse> RequestTokenAsync(HttpClient _)
            {
                // Authentication must not use a downstream client because it can have
                // Kubernetes Service Discovery handlers configured for another service.
                using var client = _httpClientFactory.CreateClient(AuthenticationHttpClientName);
                var authConfig = _configuration.GetSection(Authentication.AuthenticationSection);

                if (!bool.TryParse(authConfig[Authentication.RequireHttpsMetadata], out var requireHttps))
                    requireHttps = false;

                var discoveryDocumentRequest = new DiscoveryDocumentRequest
                {
                    Address = _authenticationEndpoint.Value.AbsoluteUri,
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

                return await client.RequestClientCredentialsTokenAsync(request);
            }
        }

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
