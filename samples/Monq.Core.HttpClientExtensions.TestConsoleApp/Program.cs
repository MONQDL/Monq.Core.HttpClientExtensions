using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monq.Core.HttpClientExtensions.TestApp;
using Monq.Core.HttpClientExtensions.TestConsoleApp;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

Console.OutputEncoding = Encoding.UTF8;

var httpContext = new DefaultHttpContext();
httpContext.Request.Headers.Append("X-C", "-1");

using var host = Host.CreateDefaultBuilder(args)
    .ConfigBasicHttpService()
    .ConfigureServices((builder, services) =>
    {
        services.AddHttpContextAccessor();
        services.Configure<ServiceUriOptions>(x => x.TestServiceUri = "https://jsonplaceholder.typicode.com");

        services.AddSingleton<IHttpContextAccessor>(_ => new HttpContextAccessor { HttpContext = httpContext });

        services.AddHttpClient<ITestService, TestService>((serviceProvider, client) =>
        {
            var baseUri = serviceProvider.GetRequiredService<IOptions<ServiceUriOptions>>().Value.TestServiceUri;
            client.BaseAddress = new(baseUri);
        });
    })
    .ConfigureLogging((builder, log) => { log.SetMinimumLevel(LogLevel.Trace); log.AddConsole(); })
    .Build();

var tasks = Enumerable.Range(1, 5).Select(i =>
{
    var scopeFactory = host.Services.GetRequiredService<IServiceScopeFactory>();
    return ExecuteService(scopeFactory, i.ToString());
});

await Task.WhenAll(tasks);

static async Task ExecuteService(IServiceScopeFactory scopeFactory, string auth)
{
    using var scope = scopeFactory.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<ITestService>();
    await service.TestApi(auth);
    await service.TestApi(auth);
}
