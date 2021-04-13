# HttpClientExtensions library for easy communication between dotnet microservices

*English*

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

The interface implementation must inherit from the class `BasicHttpService<TOptions>` or from `BasicSingleHttpService<TOptions>`.

`BasicSingleHttpService<TOptions>` is a base class that provides an out-of-the-box `BaseUri` injection mechanism for `HttpClient`.

`TOptions` is a class that is used to read settings from `asppsettings.json` for base addresses of services and is injected into `ServiceCollection` as `IOptions<TOptions>`

Class implementation:

```csharp
public class DefaultRemoteServiceApiHttpService : BasicSingleHttpService<ServiceUriOptions>, IRemoteServiceApiHttpService
{
    public DefaultRemoteServiceApiHttpService(IOptions<ServiceUriOptions> optionsAccessor, 
    ILogger<DefaultRemoteServiceApiHttpService> log,
    IHttpContextAccessor? httpContextAccessor,
    HttpMessageHandler? httpMessageInvoker = null)
        : base(optionsAccessor, log, httpContextAccessor, optionsAccessor.Value.ServiceUri, httpMessageInvoker)
    {
    }

    public async Task<IList<RemoteServiceModel>> GetAllInstances()
    {
        using var client = CreateRestHttpClient();
        
        var uri = "api/instances";
        var result = await client.Get<IList<RemoteServiceModel>>(uri, TimeSpan.FromSeconds(10));

        return result.ResultObject;
    }
}
```

Moreover, such services are implemented via DI as `scoped` services, since the service can be reused in a single ASP.NET scope. But you can easily add it as `transient`.

```csharp
public class Startup
{
	public void ConfigureServices(IServiceCollection services)
    {
		....
		services.AddOptions();
        services.Configure<ServiceUriOptions>(Configuration.GetSection("Services"));

		services.AddScoped<IRemoteServiceApiHttpService, DefaultRemoteServiceApiHttpService>();
	}
}
```

If you need to get access to other instances from the `ServiceCollection` collection inside the http service, then classic dependency injection is implemented.

```csharp
public class CachedRemoteServiceApiHttpService : BasicSingleHttpService<ServiceUriOptions>, IRemoteServiceApiHttpService
{
    readonly IMemoryCache _memoryCache;

    public CachedRemoteServiceApiHttpService(IOptions<ServiceUriOptions> optionsAccessor, 
        ILogger<CachedRemoteServiceApiHttpService> log, 
        IHttpContextAccessor? httpContextAccessor, 
        IMemoryCache memoryCache, 
        HttpMessageHandler? httpMessageInvoker = null)
        : base(optionsAccessor, log, httpContextAccessor, optionsAccessor.Value.ServiceUri, httpMessageInvoker)
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
    using var client = CreateRestHttpClient();
    
    var uri = "api/instances";
    var result = await client.Get<IList<RemoteServiceModel>>(uri, TimeSpan.FromSeconds(10));

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

    using var client = CreateRestHttpClient();
    
    var uri = "api/instances";
    var result = await client.Get<IList<RemoteServiceModel>>(uri, TimeSpan.FromSeconds(10));

    return result;
}
```

### Testing features

Http services inherited from this class are easy to test.

Add a stub class to the test project
```csharp
public class FakeResponseHandler : HttpMessageHandler
{
    readonly Dictionary<Tuple<Uri, HttpMethod>, HttpResponseMessage> _fakeResponses = new Dictionary<Tuple<Uri, HttpMethod>, HttpResponseMessage>();

    public FakeResponseHandler()
    {   }

    public void AddFakeResponse(HttpMethod httpMethod, Uri uri, HttpResponseMessage responseMessage, string content)
    {
        responseMessage.Content = new StringContent(content);
        _fakeResponses.Add(new Tuple<Uri, HttpMethod>(uri, httpMethod), responseMessage);
    }

    protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var key = new Tuple<Uri, HttpMethod>(request.RequestUri, request.Method);
        if (_fakeResponses.ContainsKey(key))
        {
            return await Task.FromResult(_fakeResponses[key]);
        }
        else
        {
            return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
        }

    }
}
```

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

		var fakeResponseHandler = new FakeResponseHandler();
		fakeResponseHandler.AddFakeResponse(HttpMethod.Get, new Uri("https://jsonplaceholder.typicode.com/api/instances"),
			new HttpResponseMessage(HttpStatusCode.OK), modelJson);

		var apiService = CreateApiService(fakeResponseHandler);

		var instances = await apiService.GetAllInstances();

        Assert.Equal(1, instances.Count())
        var firstInstance = instances.First();
		Assert.Equal(model.UserId, firstInstance.UserId);
	}

	DefaultRemoteServiceApiHttpService CreateApiService(FakeResponseHandler fakeResponseHandle)
	{
		var service = new DefaultRemoteServiceApiHttpService(_serviceUriOptionsMock.Object, _logger, null, fakeResponseHandle);
		return service;
	}
}
```

---

*Русский*

Библиотека содержит набор расширений, который применяется в проектах AspNet Core, основанных на микросервисоной архитектуре.

### Установка

```powershell
Install-Package Monq.Core.HttpClientExtensions
```

### Подключение

Библиотека может работать без явного подключения, но для некоторых параметров можно использовать конфигурацию DI. 
Например, если требуется пробрасывать заголовки из вышестоящих запросов (`HttpContext.Request`) в нижестоящие запросы, можно использовать конструкцию.

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

В обычном режиме библиотека нацелена на автоматический проброс `Bearer token` из контекста `IHttpContextAccessor`. Но в некоторых случаях, например, в консольных программах, программе самой требуется получить `Bearer token` для выполнения запросов. В таком случае требуется установить свой собственный обработчик получения токенов. Библиотека содержит реализацию по умолчанию для получения токенов из `Identity Provider`, которую можно подключить, установив ссылку на делегат своего обработчика.

```csharp
hostBuilder.ConfigureStaticAuthentication();
```
При этом требуется в Configuration наличие конфигурации вида:

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

Пример самостоятельной реализации метода аутентификации в idp используя `IdentityModel.Client`.

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

Делегат `RestHttpClient.AuthorizationRequest` потокобезопасный. Вызывается при первом вызове метода отправки http запроса. Блокировки проходят с помощью `SemaphoreSlim`. Минус такой схемы невозможность одновременной работы с несколькими ID провайдерами.


### Примеры использования

Задача: создать сервис для выполнения HTTP запросов по REST интерфейсу в формате JSON с поддержкой "проброса" авторизационного заголовка `Authentication: Bearer token`.

Для решения данной задачи требуется создать интерфейс, реализовать этот интерфейс в классе и подключить интерфейс и реализацию в DI. 
В данном случае, интерфейс позволяет легко организовать unit тестирование сервиса, который использует интерфейс.

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

Реализация интерфейса должна наследоваться от класса `BasicHttpService<TOptions>` или от `BasicSingleHttpService<TOptions>`.

`BasicSingleHttpService<TOptions>` - это базовый класс, который предоставляет уже готовый механизм внедрения `BaseUri` для `HttpClient`.

`TOptions` - это класс, который используется для чтения настроек из asppsettings.json базовых адресов сервисов и инжектирован в `ServiceCollection` как `IOptions<TOptions>`

Реализация класса:

```csharp
public class DefaultRemoteServiceApiHttpService : BasicSingleHttpService<ServiceUriOptions>, IRemoteServiceApiHttpService
{
    public DefaultRemoteServiceApiHttpService(IOptions<ServiceUriOptions> optionsAccessor, 
    ILogger<DefaultRemoteServiceApiHttpService> log,
    IHttpContextAccessor? httpContextAccessor,
    HttpMessageHandler? httpMessageInvoker = null)
        : base(optionsAccessor, log, httpContextAccessor, optionsAccessor.Value.ServiceUri, httpMessageInvoker)
    {
    }

    public async Task<IList<RemoteServiceModel>> GetAllInstances()
    {
        using var client = CreateRestHttpClient();
        
        var uri = "api/instances";
        var result = await client.Get<IList<RemoteServiceModel>>(uri, TimeSpan.FromSeconds(10));

        return result.ResultObject;
    }
}
```

При этом такие сервисы внедряются через DI как `scoped` сервисы, т.к. сервис может быть переиспользован в едином scope ASP.NET.

```csharp
public class Startup
{
	public void ConfigureServices(IServiceCollection services)
    {
		....
		services.AddOptions();
        services.Configure<ServiceUriOptions>(Configuration.GetSection("Services"));

		services.AddScoped<IRemoteServiceApiHttpService, DefaultRemoteServiceApiHttpService>();
	}
}
```

Если требуется внутри http сервиса получить доступ к другим экземплярам из коллекции `ServiceCollection`, то реализуется классическое внедрение зависимостей.

```csharp
public class CachedRemoteServiceApiHttpService : BasicSingleHttpService<ServiceUriOptions>, IRemoteServiceApiHttpService
{
    readonly IMemoryCache _memoryCache;

    public CachedRemoteServiceApiHttpService(IOptions<ServiceUriOptions> optionsAccessor, 
        ILogger<CachedRemoteServiceApiHttpService> log, 
        IHttpContextAccessor? httpContextAccessor, 
        IMemoryCache memoryCache, 
        HttpMessageHandler? httpMessageInvoker = null)
        : base(optionsAccessor, log, httpContextAccessor, optionsAccessor.Value.ServiceUri, httpMessageInvoker)
    {
        _memoryCache = memoryCache;
    }
	..........
}
```

### Особенности реализации

- В конструкторе класса требуется указать тип самого класса, при объявлении `ILogger`.

```csharp
ILogger<DefaultRemoteServiceApiHttpService> log
```

- В некоторых ситуациях требуется отдавать полный ответ от микросервиса, включающий заголовки ответа. 
```csharp
public async Task<RestHttpResponseMessage<IList<RemoteServiceModel>> GetAllInstances()
{
    using var client = CreateRestHttpClient();
    
    var uri = "api/instances";
    var result = await client.Get<IList<RemoteServiceModel>>(uri, TimeSpan.FromSeconds(10));

    return result;
}
```
- Если в методе, который должен возвращать `RestHttpResponseMessage<T>` требуется выполнить ранний выход еще до выполнения запроса, можно использовать обертку `RestHttpResponseMessageWrapper.Empty<>`, которая вернет тип `RestHttpResponseMessage<T>`. Пример:
```csharp
using Monq.Core.HttpClientExtensions.Extensions;

public async Task<RestHttpResponseMessage<IList<RemoteServiceModel>> FilterInstances(RemoteServiceFilter filter)
{
    if (filter is null || filter.Prop is null)
        return RestHttpResponseMessageWrapper.Empty<IEnumerable<ConnectorMinimalViewModel>>();

    using var client = CreateRestHttpClient();
    
    var uri = "api/instances";
    var result = await client.Get<IList<RemoteServiceModel>>(uri, TimeSpan.FromSeconds(10));

    return result;
}
```

### Особенности тестирования

Http сервисы, наследуемые от данного класса легко тестируются.

Добавляется класс-заглушка в тестовый проект
```csharp
public class FakeResponseHandler : HttpMessageHandler
{
    readonly Dictionary<Tuple<Uri, HttpMethod>, HttpResponseMessage> _fakeResponses = new Dictionary<Tuple<Uri, HttpMethod>, HttpResponseMessage>();

    public FakeResponseHandler()
    {   }

    public void AddFakeResponse(HttpMethod httpMethod, Uri uri, HttpResponseMessage responseMessage, string content)
    {
        responseMessage.Content = new StringContent(content);
        _fakeResponses.Add(new Tuple<Uri, HttpMethod>(uri, httpMethod), responseMessage);
    }

    protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var key = new Tuple<Uri, HttpMethod>(request.RequestUri, request.Method);
        if (_fakeResponses.ContainsKey(key))
        {
            return await Task.FromResult(_fakeResponses[key]);
        }
        else
        {
            return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
        }

    }
}
```

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

		var fakeResponseHandler = new FakeResponseHandler();
		fakeResponseHandler.AddFakeResponse(HttpMethod.Get, new Uri("https://jsonplaceholder.typicode.com/api/instances"),
			new HttpResponseMessage(HttpStatusCode.OK), modelJson);

		var apiService = CreateApiService(fakeResponseHandler);

		var instances = await apiService.GetAllInstances();

        Assert.Equal(1, instances.Count())
        var firstInstance = instances.First();
		Assert.Equal(model.UserId, firstInstance.UserId);
	}

	DefaultRemoteServiceApiHttpService CreateApiService(FakeResponseHandler fakeResponseHandle)
	{
		var service = new DefaultRemoteServiceApiHttpService(_serviceUriOptionsMock.Object, _logger, null, fakeResponseHandle);
		return service;
	}
}
```