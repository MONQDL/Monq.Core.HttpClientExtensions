using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monq.Core.HttpClientExtensions.Services;
using System.Net.Http;
using System.Threading.Tasks;

namespace Monq.Core.HttpClientExtensions.TestApp
{
    public interface ITestService
    {
        Task<TestModel> TestApi();
    }

    public class TestService : BasicSingleHttpService<ServiceUriOptions>, ITestService
    {
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
            ILoggerFactory loggerFactory,
            BasicHttpServiceOptions configuration,
            IHttpContextAccessor httpContextAccessor,
            HttpMessageHandler? httpMessageInvoker = null) : 
            base(optionsAccessor, 
                loggerFactory, 
                configuration, 
                httpContextAccessor, 
                optionsAccessor.Value.TestServiceUri, 
                httpMessageInvoker)
        {
        }

        public async Task<TestModel> TestApi()
        {
            using var client = CreateRestHttpClient();
            var result = await client.Get<TestModel>("posts/1");

            return result.ResultObject;
        }
    }
}
