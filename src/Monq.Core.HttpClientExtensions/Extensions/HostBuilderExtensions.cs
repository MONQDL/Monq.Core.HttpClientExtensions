using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Monq.Core.HttpClientExtensions;
using Monq.Core.MicroserviceAddressing.Addressing;
using System;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for the generic app host builder (<see cref = "IHostBuilder" />).
    /// </summary>
    public static class HostBuilderExtensions
    {

        /// <summary>
        /// The authentication service application name used by the parameterless
        /// <see cref="ConfigureStaticAuthentication(IHostBuilder)" /> overloads.
        /// </summary>
        public const string DefaultAuthenticationApplicationName = "pl-identity-service";

        /// <summary>
        /// Configure a static authentication method for the default authentication service
        /// (<see cref="DefaultAuthenticationApplicationName" />). The endpoint is resolved with the
        /// configured HTTP microservice address resolver. Repeated calls do not register
        /// additional authentication hosted services.
        /// </summary>
        /// <param name="hostBuilder">The host builder.</param>
        public static IHostBuilder ConfigureStaticAuthentication(this IHostBuilder hostBuilder) =>
            ConfigureStaticAuthentication(hostBuilder, DefaultAuthenticationApplicationName);

        /// <summary>
        /// Configure a static authentication method for the default authentication service
        /// (<see cref="DefaultAuthenticationApplicationName" />) with custom settings for the
        /// isolated authentication HTTP client. The endpoint is resolved with the configured
        /// HTTP microservice address resolver.
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
                DefaultAuthenticationApplicationName,
                configureAuthenticationClient);
        }

        /// <summary>
        /// Configure a static authentication method using an explicit absolute authentication endpoint.
        /// </summary>
        /// <param name="hostBuilder">The host builder.</param>
        /// <param name="authenticationEndpoint">The absolute authentication endpoint.</param>
        /// <param name="configureAuthenticationClient">
        /// Optional configuration for the isolated authentication HTTP client.
        /// </param>
        public static IHostBuilder ConfigureStaticAuthentication(
            this IHostBuilder hostBuilder,
            Uri authenticationEndpoint,
            Action<IHttpClientBuilder>? configureAuthenticationClient = null)
        {
            ArgumentNullException.ThrowIfNull(authenticationEndpoint);
            if (!authenticationEndpoint.IsAbsoluteUri)
                throw new ArgumentException(
                    "The authentication endpoint must be an absolute URI.",
                    nameof(authenticationEndpoint));

            return ConfigureStaticAuthentication(hostBuilder, _ => authenticationEndpoint,
                configureAuthenticationClient);
        }

        /// <summary>
        /// Configure a static authentication method using the authentication service address
        /// returned by the configured HTTP microservice address resolver.
        /// </summary>
        /// <param name="hostBuilder">The host builder.</param>
        /// <param name="applicationName">The authentication service application name.</param>
        /// <param name="configureAuthenticationClient">
        /// Optional configuration for the isolated authentication HTTP client.
        /// </param>
        /// <param name="port">Optional service port override.</param>
        /// <param name="addressResolver">Optional resolver override for this authentication service.</param>
        public static IHostBuilder ConfigureStaticAuthentication(
            this IHostBuilder hostBuilder,
            string applicationName,
            Action<IHttpClientBuilder>? configureAuthenticationClient = null,
            int? port = null,
            IMicroserviceAddressResolver? addressResolver = null)
        {
            ArgumentNullException.ThrowIfNull(hostBuilder);
            if (string.IsNullOrWhiteSpace(applicationName))
                throw new ArgumentException("The application name cannot be empty.", nameof(applicationName));

            hostBuilder.ConfigureServices(services => services.AddMicroserviceAddressing());

            return ConfigureStaticAuthentication(
                hostBuilder,
                provider => provider
                    .GetRequiredService<IMicroserviceAddressProvider>()
                    .Resolve(
                        applicationName,
                        MicroserviceProtocol.Http,
                        port,
                        addressResolver)
                    .Address,
                configureAuthenticationClient);
        }

        static IHostBuilder ConfigureStaticAuthentication(
            this IHostBuilder hostBuilder,
            Func<IServiceProvider, Uri> authenticationEndpointFactory,
            Action<IHttpClientBuilder>? configureAuthenticationClient = null)
        {
            ArgumentNullException.ThrowIfNull(hostBuilder);
            ArgumentNullException.ThrowIfNull(authenticationEndpointFactory);

            return hostBuilder.ConfigureServices((_, services) =>
            {
                // The first call configures the endpoint, the isolated client, and the hosted
                // service; repeated calls keep the existing registrations.
                if (services.Any(static descriptor =>
                        descriptor.ServiceType == typeof(StaticAuthenticationService)))
                    return;

                var clientBuilder = services.AddHttpClient(
                    StaticAuthenticationService.HttpClientName,
                    client => client.Timeout = StaticAuthenticationService.DefaultTimeout);
                configureAuthenticationClient?.Invoke(clientBuilder);

                services.AddSingleton(provider =>
                {
                    var authenticationEndpoint = authenticationEndpointFactory(provider);
                    ArgumentNullException.ThrowIfNull(authenticationEndpoint);
                    if (!authenticationEndpoint.IsAbsoluteUri)
                        throw new ArgumentException(
                            "The authentication endpoint must be an absolute URI.",
                            nameof(authenticationEndpointFactory));

                    return new StaticAuthenticationEndpoint(authenticationEndpoint);
                });

                services.AddSingleton<StaticAuthenticationService>();
                services.AddSingleton<IHostedService>(static provider =>
                    provider.GetRequiredService<StaticAuthenticationService>());
            });
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
