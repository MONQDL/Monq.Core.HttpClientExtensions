# Библиотека с расширениями класса HttpClient для поддержки микросервисов .net core

Библиотека содержит набор расширений, который применяется в проектах AspNet Core, основанных на микросервисоной архитектуре.

### Установка

```powershell
Install-Package Monq.Core.HttpClientExtensions
```

### Подключение

Библиотека может работать без явного подключения, но для некоторых параметров можно использовать конфигурацию DI.

*Startup.cs*

```csharp
hostBuilder.ConfigBasicHttpService(opts =>
{
    var headerOptions = new BasicHttpServiceHeaderOptions();
    headerOptions.AddForwardedHeader(MicroserviceConstants.EventIdHeader);
    headerOptions.AddForwardedHeader(MicroserviceConstants.UserspaceIdHeader);

    opts.ConfigHeaders(headerOptions);
});

```

### Примеры использования

Задача: создать сервис для выполнения HTTP запросов по REST интерфейсу в формате JSON с поддержкой "проброса" авторизационного заголовка `Authentication: Bearer token`.

Для решения данной задачи требуется создать интерфейс.

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

При этом такие сервисы внедряются через DI как AddScoped сервисы, т.к. сервис может быть переиспользован в едином scope ASP.NET.

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

Если требуется внутри Http сервиса получить доступ к другим экземплярам из коллекции `ServiceCollection`, то реализуется классическое внедрение зависимостей.

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

```
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