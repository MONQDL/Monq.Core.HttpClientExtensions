using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Monq.Core.HttpClientExtensions.TestApp;

public interface ITestService
{
    Task<TestModel> TestApi();
}

public class TestService : RestHttpClient, ITestService
{
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
    }

    public async Task<TestModel> TestApi()
    {
        var result = await Get<TestModel>("posts/1", TimeSpan.FromSeconds(10),
            serializer: RestHttpClientSystemTextJsonSerializer.Default);

        return result.ResultObject!;
    }
}
