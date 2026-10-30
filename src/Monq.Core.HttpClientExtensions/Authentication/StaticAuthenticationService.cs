using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monq.Core.HttpClientExtensions.Exceptions;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static Monq.Core.HttpClientExtensions.AuthConstants;

namespace Monq.Core.HttpClientExtensions
{
    sealed class StaticAuthenticationService : IHostedService
    {
        /// <summary>
        /// Name of the isolated <see cref="HttpClient" /> used to obtain the system token.
        /// </summary>
        internal const string HttpClientName = "Monq.Core.HttpClientExtensions.StaticAuthentication";

        /// <summary>
        /// Default timeout of the isolated authentication <see cref="HttpClient" />.
        /// </summary>
        internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

        const string WriteScope = "write";

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
            using var client = _httpClientFactory.CreateClient(HttpClientName);
            var authConfig = _configuration.GetSection(Authentication.AuthenticationSection);

            var clientId = authConfig[Authentication.ClientName];
            if (string.IsNullOrWhiteSpace(clientId))
                throw new MissingConfigurationException(
                    $"Configuration value '{Authentication.AuthenticationSection}:{Authentication.ClientName}' "
                    + "is required for static authentication.");

            var clientSecret = authConfig[Authentication.ClientSecret];
            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new MissingConfigurationException(
                    $"Configuration value '{Authentication.AuthenticationSection}:{Authentication.ClientSecret}' "
                    + "is required for static authentication.");

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
                ClientId = clientId,
                ClientSecret = clientSecret,
                Scope = WriteScope
            };

            return await client.RequestClientCredentialsTokenAsync(request);
        }
    }
}
