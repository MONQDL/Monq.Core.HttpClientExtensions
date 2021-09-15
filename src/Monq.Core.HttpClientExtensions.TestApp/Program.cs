using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Monq.Core.HttpClientExtensions.TestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigBasicHttpService(opts =>
                {
                    var headerOptions = new RestHttpClientHeaderOptions();
                    headerOptions.AddForwardedHeader("X-Trace-Event-Id");
                    headerOptions.AddForwardedHeader("Accept-Language");
                    opts.ConfigHeaders(headerOptions);
                })
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
    }
}
