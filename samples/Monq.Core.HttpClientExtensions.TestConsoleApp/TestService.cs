using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Monq.Core.HttpClientExtensions.TestConsoleApp;

public interface ITestService
{
    Task<TestModel> TestApi(string auth);
}

public class TestService : RestHttpClient, ITestService
{
    readonly ILogger<TestService> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TestService" />.
    /// </summary>
    public TestService(
        HttpClient httpClient,
        ILoggerFactory loggerFactory,
        RestHttpClientOptions configuration,
        IHttpContextAccessor httpContextAccessor)
        : base(
            httpClient,
            loggerFactory,
            configuration,
            httpContextAccessor)
    {
        _logger = loggerFactory.CreateLogger<TestService>();
    }

    public async Task<TestModel> TestApi(string auth)
    {
        _logger.LogInformation(JsonSerializer.Serialize(HttpClient.DefaultRequestHeaders));

        var headers = new HeaderDictionary
        {
            { "Authorization", $"Bearer {auth}" }
        };

        var result = await Get<TestModel>("posts/1", TimeSpan.FromSeconds(10), headers);

        return result.ResultObject!;
    }
}
