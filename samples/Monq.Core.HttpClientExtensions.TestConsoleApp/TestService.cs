using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monq.Core.HttpClientExtensions.TestApp;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Monq.Core.HttpClientExtensions.TestConsoleApp
{
    public interface ITestService
    {
        Task<TestModel> TestApi(string auth);
    }

    public class TestService : RestHttpClientFromOptions<ServiceUriOptions>, ITestService
    {
        ILogger<TestService> _log;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="TestService" />.
        /// </summary>
        /// <param name="optionsAccessor">The options.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="httpMessageInvoker">The HTTP message invoker.</param>
        /// <exception cref="System.ArgumentNullException">baseUri - Не указан базовый Uri сервиса.</exception>
        public TestService(
            IOptions<ServiceUriOptions> optionsAccessor,
            HttpClient httpClient,
            ILoggerFactory loggerFactory,
            RestHttpClientOptions configuration,
            IHttpContextAccessor httpContextAccessor) :
            base(optionsAccessor,
                httpClient,
                loggerFactory,
                configuration,
                httpContextAccessor,
                optionsAccessor.Value.TestServiceUri)
        {
            _log = loggerFactory.CreateLogger<TestService>();
        }

        public async Task<TestModel> TestApi(string auth)
        {
            //HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {auth}");

            _log.LogInformation(System.Text.Json.JsonSerializer.Serialize(HttpClient.DefaultRequestHeaders));

            var headers = new HeaderDictionary();
            headers.Add("Authorization", $"Bearer {auth}");

            var result = await Get<TestModel>("posts/1", TimeSpan.FromSeconds(10), headers);

            return result.ResultObject!;
        }
    }
}
