using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Net.Http;
using Xunit;

namespace Monq.Core.HttpClientExtensions.Tests;

public class HostBuilderExtensionsTests
{
    const string AuthenticationHttpClientName =
        "Monq.Core.HttpClientExtensions.StaticAuthentication";

    [Fact(DisplayName = "Use a ten second timeout for static authentication by default.")]
    public void ShouldUseDefaultStaticAuthenticationTimeout()
    {
        using var host = new HostBuilder()
            .ConfigureStaticAuthentication(_ => new Uri("https://identity.example.com"))
            .Build();

        var factory = host.Services.GetRequiredService<IHttpClientFactory>();
        using var client = factory.CreateClient(AuthenticationHttpClientName);

        Assert.Equal(TimeSpan.FromSeconds(10), client.Timeout);
    }

    [Fact(DisplayName = "Configure an isolated static authentication client.")]
    public void ShouldConfigureIsolatedStaticAuthenticationClient()
    {
        var expectedTimeout = TimeSpan.FromSeconds(17);
        using var host = new HostBuilder()
            .ConfigureStaticAuthentication(
                _ => new Uri("https://identity.example.com"),
                clientBuilder => clientBuilder.ConfigureHttpClient(client =>
                    client.Timeout = expectedTimeout))
            .Build();

        var factory = host.Services.GetRequiredService<IHttpClientFactory>();
        using var client = factory.CreateClient(AuthenticationHttpClientName);

        Assert.Equal(expectedTimeout, client.Timeout);
    }

    [Fact(DisplayName = "Register static authentication hosted service once.")]
    public void ShouldRegisterStaticAuthenticationHostedServiceOnce()
    {
        using var host = new HostBuilder()
            .ConfigureStaticAuthentication(_ => new Uri("https://identity.example.com"))
            .ConfigureStaticAuthentication(_ => new Uri("https://identity.example.com"))
            .Build();

        var authenticationServices = host.Services
            .GetServices<IHostedService>()
            .Where(service => service.GetType().Name == "StaticAuthenticationService");

        Assert.Single(authenticationServices);
    }
}
