using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monq.Core.HttpClientExtensions.TestApp;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monq.Core.HttpClientExtensions.TestConsoleApp
{
    class Program
    {
        static readonly DefaultHttpContext _httpContext = new DefaultHttpContext();
        
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            _httpContext.Request.Headers.Add("X-C", "-1");

            var hostBuilder = new HostBuilder()
                .ConfigBasicHttpService()
                .ConfigureServices((host, services) =>
                {
                    services.AddHttpContextAccessor();
                    services.Configure<ServiceUriOptions>(x => x.TestServiceUri = "https://jsonplaceholder.typicode.com");

                    services.AddHttpClient<ITestService, TestService>();
                })
                .ConfigureLogging((host, log) => { log.SetMinimumLevel(LogLevel.Trace); log.AddConsole(); })
                .Build();

            var httpContextAccessor = hostBuilder.Services.GetRequiredService<IHttpContextAccessor>();
            httpContextAccessor.HttpContext = _httpContext;

            var tasks = Enumerable.Range(1, 5).Select(i =>
            {
                var scopeFactory = hostBuilder.Services.GetRequiredService<IServiceScopeFactory>();
                return ExecuteService(scopeFactory, i.ToString());
            });

            await Task.WhenAll(tasks);
        }

        static async Task ExecuteService(IServiceScopeFactory scopeFactory, string auth)
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITestService>();
            await service.TestApi(auth);
            await service.TestApi(auth);
        }
    }
}
