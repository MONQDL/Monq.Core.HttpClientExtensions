using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monq.Core.MicroserviceAddressing.Addressing;
using Monq.Core.MicroserviceAddressing.Hosting;
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
            .ConfigureStaticAuthentication(new Uri("https://identity.example.com"))
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
                new Uri("https://identity.example.com"),
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
            .ConfigureStaticAuthentication(new Uri("https://identity.example.com"))
            .ConfigureStaticAuthentication(new Uri("https://identity.example.com"))
            .Build();

        var authenticationServices = host.Services
            .GetServices<IHostedService>()
            .Where(service => service.GetType().Name == "StaticAuthenticationService");

        Assert.Single(authenticationServices);
    }

    [Fact(DisplayName = "Resolve the static authentication endpoint by application name.")]
    public void ShouldResolveStaticAuthenticationEndpointByApplicationName()
    {
        var resolver = new CapturingAddressResolver();

        using var host = new HostBuilder()
            .UseHttpMicroserviceAddressResolver(resolver)
            .ConfigureStaticAuthentication("identity-service", port: 8080)
            .Build();

        Assert.NotNull(resolver.Context);
        Assert.Equal("identity-service", resolver.Context.TargetApplicationName);
        Assert.Equal(MicroserviceProtocol.Http, resolver.Context.Protocol);
        Assert.Equal(8080, resolver.Context.PortOverride);
    }

    [Fact(DisplayName = "Use the default authentication application name for parameterless configuration.")]
    public void ShouldUseDefaultAuthenticationApplicationName()
    {
        var resolver = new CapturingAddressResolver();

        using var host = new HostBuilder()
            .UseHttpMicroserviceAddressResolver(resolver)
            .ConfigureStaticAuthentication()
            .Build();

        Assert.NotNull(resolver.Context);
        Assert.Equal(
            HostBuilderExtensions.DefaultAuthenticationApplicationName,
            resolver.Context.TargetApplicationName);
    }

    [Fact(DisplayName = "Apply client configuration with the default authentication application name.")]
    public void ShouldConfigureClientWithDefaultAuthenticationApplicationName()
    {
        var resolver = new CapturingAddressResolver();
        var expectedTimeout = TimeSpan.FromSeconds(23);

        using var host = new HostBuilder()
            .UseHttpMicroserviceAddressResolver(resolver)
            .ConfigureStaticAuthentication((Action<IHttpClientBuilder>)(clientBuilder =>
                clientBuilder.ConfigureHttpClient(client => client.Timeout = expectedTimeout)))
            .Build();

        Assert.NotNull(resolver.Context);
        Assert.Equal(
            HostBuilderExtensions.DefaultAuthenticationApplicationName,
            resolver.Context.TargetApplicationName);

        var factory = host.Services.GetRequiredService<IHttpClientFactory>();
        using var client = factory.CreateClient(AuthenticationHttpClientName);

        Assert.Equal(expectedTimeout, client.Timeout);
    }

    sealed class CapturingAddressResolver : IMicroserviceAddressResolver
    {
        public MicroserviceAddressContext? Context { get; private set; }

        public ResolvedMicroserviceAddress Resolve(MicroserviceAddressContext context)
        {
            Context = context;
            return new(new Uri("https://identity.example.com"));
        }
    }
}
