using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Monq.Core.HttpClientExtensions.Tests;

[Collection("StaticAuthentication")]
public class StaticAuthenticationActivationTests
{
    [Fact(DisplayName = "Activate static authentication for a token requested before the host is started.")]
    public async Task ShouldActivateStaticAuthenticationBeforeHostStart()
    {
        RestHttpClient.ResetAuthorizationRequestHandler();
        RestHttpClient.ResetAccessToken();

        try
        {
            using var host = new HostBuilder()
                .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Authentication:Client:Login"] = "test-client",
                        ["Authentication:Client:Password"] = "test-secret"
                    }))
                .ConfigureServices(services =>
                {
                    services.AddLogging();
                    services.AddHttpContextAccessor();
                    services.AddSingleton(new RestHttpClientOptions());
                    services.AddHttpClient<RestHttpClient>();
                })
                .ConfigureStaticAuthentication(
                    new Uri("http://identity.test"),
                    clientBuilder => clientBuilder.ConfigurePrimaryHttpMessageHandler(
                        () => new StubIdentityHandler()))
                .Build();

            var restHttpClient = host.Services.GetRequiredService<RestHttpClient>();

            var token = await restHttpClient.GetAccessToken(false);

            Assert.Equal("system-token", token?.AccessToken);
        }
        finally
        {
            RestHttpClient.ResetAuthorizationRequestHandler();
            RestHttpClient.ResetAccessToken();
        }
    }

    sealed class StubIdentityHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.RequestUri!.AbsolutePath switch
            {
                "/.well-known/openid-configuration" => """
                    {
                      "issuer": "http://identity.test",
                      "authorization_endpoint": "http://identity.test/connect/authorize",
                      "token_endpoint": "http://identity.test/connect/token",
                      "jwks_uri": "http://identity.test/.well-known/jwks.json"
                    }
                    """,
                "/connect/token" => """
                    { "access_token": "system-token", "token_type": "Bearer", "expires_in": 3600 }
                    """,
                _ => "{}"
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
