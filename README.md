# HttpClientExtensions library for easy communication between dotnet microservices

The library provides a set of extensions to the HttpClient class to support .net core microservices.

### Installation

```powershell
Install-Package Monq.Core.HttpClientExtensions
```

### Using the library

The library can work without explicit connection, but for some parameters it is possible to use DI configuration.

```csharp
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigBasicHttpService()
        .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
```

If you want to forward headers from upstream requests (`HttpContext.Request`) to downstream requests, you can use configuration options.

*Program.cs*

```csharp
hostBuilder.ConfigBasicHttpService(opts =>
{
    var headerOptions = new BasicHttpServiceHeaderOptions();
    headerOptions.AddForwardedHeader(MicroserviceConstants.EventIdHeader);
    headerOptions.AddForwardedHeader(MicroserviceConstants.UserspaceIdHeader);

    opts.ConfigHeaders(headerOptions);
});

```

In default mode, the library uses the `Newtonsoft.Json` serializer, but it is possible to switch to `System.Text.Json`. Example:

*Startup.cs*
```csharp
{
	public void ConfigureServices(IServiceCollection services)
    {
		....
		RestHttpClientSerializer.UseSystemTextJson(options => options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase); // Use System.Text.Json with options.
        RestHttpClientSerializer.UseNewtonsoftJson(); // Use NewtonsoftJson. Default.
	}
}
```

The default serializer options are:

*Newtonsoft.Json*
```csharp
new Newtonsoft.Json.JsonSerializerSettings() { ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver
{
    NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy
    {
        ProcessDictionaryKeys = true
    }
}};
```

*System.Text.Json*
```csharp
new System.Text.Json.JsonSerializerOptions
{
    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
    DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
};
```

In normal mode, the library aims to automatically forward the `Bearer token` from the `IHttpContextAccessor` context. But in some cases, for example, in console programs, the program itself needs to get a `Bearer token` in order to execute requests. In this case, you need to set your own handler for requesting tokens. The library contains a default implementation for getting tokens from the `oidc` provider, which can be connected by setting a reference to your handler delegate.

```csharp
hostBuilder.ConfigureStaticAuthentication();
```
In this case you must set configuration from appsettings.json. The library searches for the *"Authentication"* token.

*appsettings.json*
```json
{
  "Authentication": {
    "AuthenticationEndpoint": "https://smon.monq.ru",
    "Client": {
        "Login": "idp-client",
        "Password": "idp-client-secret"
    },
    "RequireHttpsMetadata": false
  }
}
```

An example of self-implementation of the authentication method in idp using `IdentityModel.Client`.

*Program.cs*
```csharp
using IdentityModel.Client;
using Monq.Core.HttpClientExtensions.Exceptions;
using System.Net.Http;
using System.Threading.Tasks;

...

RestHttpClient.AuthorizationRequest += RestHttpClientAuthorizationRequest;

static async Task<TokenResponse> RestHttpClientAuthorizationRequest(HttpClient client)
{
    var authEndpoint = "";
    var requireHttps = true;
    var clientId = "";
    var clientSecret = "";
    var scope = "scope1 scope2";

    var discoveryDocumentRequest = new DiscoveryDocumentRequest
    {
        Address = authEndpoint,
        Policy = new DiscoveryPolicy { RequireHttps = requireHttps }
    };
    var disco = await client.GetDiscoveryDocumentAsync(discoveryDocumentRequest);
    if (disco.IsError) throw new DiscoveryEndpointException(disco.Error, disco.Exception);

    var request = new ClientCredentialsTokenRequest
    {
        Address = disco.TokenEndpoint,
        ClientId = clientId,
        ClientSecret = clientSecret,
        Scope = scope
    };

    var response = await client.RequestClientCredentialsTokenAsync(request);
    return response;
}
```

The `RestHttpClient.AuthorizationRequest` delegate is thread safe. It called at the first time the http request sending method is called or on the 401 http response. Locks are passed using `SemaphoreSlim`. The disadvantage of this scheme is the impossibility of simultaneous work with several ID providers.


### Using examples

Objective: create a service for executing HTTP requests via REST interface in JSON format with support for "forwarding" the authorization header `Authentication: Bearer token`.

To solve this problem, you need to create an interface, implement this interface in a class, and connect the interface and implementation in DI.
In this case, the interface makes it easy to unit test the service that uses the interface.

*ServiceUriOptions.cs*
```csharp
public class ServiceUriOptions
{
    public string ServiceUri { get; set; } = default!;
}
```

*RemoteServiceModel.cs*
```csharp
public class RemoteServiceModel
{
    public int UserId { get; set; }
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
}
```

*IRemoteServiceApiHttpService.cs*
```csharp
public interface IRemoteServiceApiHttpService
{
    Task<IList<RemoteServiceModel>> GetAllInstances();
}
```

The interface implementation must inherit from the class `RestHttpClient` or from `RestHttpClientFromOptions<TOptions>`.

`RestHttpClientFromOptions<TOptions>` is a base class that provides an out-of-the-box `BaseUri` injection mechanism for `HttpClient`.

`TOptions` is a class that is used to read settings from `asppsettings.json` for base addresses of services and is injected into `ServiceCollection` as `IOptions<TOptions>`

Class implementation:

```csharp
public class DefaultRemoteServiceApiHttpService : RestHttpClientFromOptions<ServiceUriOptions>, IRemoteServiceApiHttpService
{
    public DefaultRemoteServiceApiHttpService(IOptions<ServiceUriOptions> optionsAccessor,
            HttpClient httpClient,
            ILoggerFactory loggerFactory,
            BasicHttpServiceOptions configuration,
            IHttpContextAccessor httpContextAccessor)
        : base(optionsAccessor,
                httpClient,
                loggerFactory,
                configuration,
                httpContextAccessor,
                optionsAccessor.Value.ServiceUri)
    {
    }

    public async Task<IList<RemoteServiceModel>> GetAllInstances()
    {
        var uri = "api/instances";
        var result = await Get<IList<RemoteServiceModel>>(uri, TimeSpan.FromSeconds(10));

        return result.ResultObject;
    }
}
```

Moreover, such services are implemented via DI as HttpClient services and they must be added to DI over `AddHttpClient<>()` method.

```csharp
public class Startup
{
	public void ConfigureServices(IServiceCollection services)
    {
		....
		services.AddOptions();
        services.Configure<ServiceUriOptions>(Configuration.GetSection("Services"));

		services.AddHttpClient<IRemoteServiceApiHttpService, DefaultRemoteServiceApiHttpService>();
	}
}
```

If you need to get access to other instances from the `ServiceCollection` collection inside the http service, then classic dependency injection is implemented.

```csharp
public class CachedRemoteServiceApiHttpService : RestHttpClientFromOptions<ServiceUriOptions>, IRemoteServiceApiHttpService
{
    readonly IMemoryCache _memoryCache;

    public CachedRemoteServiceApiHttpService(IOptions<ServiceUriOptions> optionsAccessor,
            HttpClient httpClient,
            ILoggerFactory loggerFactory,
            BasicHttpServiceOptions configuration,
            IHttpContextAccessor httpContextAccessor 
            IMemoryCache memoryCache)
        : base(optionsAccessor,
                httpClient,
                loggerFactory,
                configuration,
                httpContextAccessor,
                optionsAccessor.Value.ServiceUri)
    {
        _memoryCache = memoryCache;
    }
	..........
}
```

### Implementation features

- In the constructor of the class, you must specify the type of the class itself, when declaring `ILogger`.

```csharp
ILogger<DefaultRemoteServiceApiHttpService> log
```

- In some situations, it is required to give a complete response from the microservice, including response headers.
```csharp
public async Task<RestHttpResponseMessage<IList<RemoteServiceModel>> GetAllInstances()
{
    var uri = "api/instances";
    var result = await Get<IList<RemoteServiceModel>>(uri, TimeSpan.FromSeconds(10));

    return result;
}
```
- If you want to exit early in the method that should return `RestHttpResponseMessage<T>` before executing the request, you can use the `RestHttpResponseMessageWrapper.Empty<T>` wrapper, which will return the type `RestHttpResponseMessage<T>`. Example:
```csharp
using Monq.Core.HttpClientExtensions.Extensions;

public async Task<RestHttpResponseMessage<IList<RemoteServiceModel>> FilterInstances(RemoteServiceFilter filter)
{
    if (filter is null || filter.Prop is null)
        return RestHttpResponseMessageWrapper.Empty<IEnumerable<ConnectorMinimalViewModel>>(); // using the response wrapper.

    var uri = "api/instances";
    var result = await Get<IList<RemoteServiceModel>>(uri, TimeSpan.FromSeconds(10));

    return result;
}
```

### Using Polly

As the v5 library now uses the HttpClientFactory, you can easily use `Microsoft.Extensions.Http.Polly` library. Just add it to the project via the Nuget and choose needed policies.

```csharp
using Polly;
using Polly.Extensions.Http;

public class Startup
{
	public void ConfigureServices(IServiceCollection services)
    {
		....
		services.AddHttpClient<IRemoteServiceApiHttpService, DefaultRemoteServiceApiHttpService>()
            .AddPolicyHandler(GetCircuitBreakerPolicy());
	}

    static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(2, TimeSpan.FromSeconds(30));
    }
}
```

### Testing features

Http services inherited from this class are easy to test.

```csharp
public class DefaultRemoteServiceApiHttpServiceTests
{
	readonly ILogger<DefaultRemoteServiceApiHttpService> _logger;
	readonly Mock<IOptions<ServiceUriOptions>> _serviceUriOptionsMock;

	public DefaultRemoteServiceApiHttpServiceTests()
    {
        _logger = new StubLogger<DefaultRemoteServiceApiHttpService>();

        _serviceUriOptionsMock = new Mock<IOptions<ServiceUriOptions>>();
        _serviceUriOptionsMock.Setup(x => x.Value)
            .Returns(new ServiceUriOptions() {
                ServiceUri = "https://jsonplaceholder.typicode.com"
            });
    }

    [Fact]
	public async Task ShouldProperlyGetAllInstances()
	{
		var model = new RemoteServiceModel() { UserId = 1532 };
		var modelJson = JsonConvert.SerializeObject(new List<RemoteServiceModel>() { model });

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(modelJson),
            });

        var client = new HttpClient(mockHttpMessageHandler.Object);

		var apiService = CreateApiService(client);

		var instances = await apiService.GetAllInstances();

        Assert.Equal(1, instances.Count())
        var firstInstance = instances.First();
		Assert.Equal(model.UserId, firstInstance.UserId);
	}

	DefaultRemoteServiceApiHttpService CreateApiService(HttpClient httpClient, HttpContext? httpContext, IOptions<ServiceOptions> optionsAccessor)
	{
		return new DefaultRemoteServiceApiHttpService(optionsAccessor ?? _optionsMoq.Object,
                       httpClient,
                       _loggerFactory,
                       null,
                       new HttpContextAccessorStub(httpContext ?? new DefaultHttpContext()),
                       optionsAccessor.Value.ServiceUri);
	}
}
```