using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Monq.Core.HttpClientExtensions;
using Monq.Core.HttpClientExtensions.TestApp;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Net.Http;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
Console.OutputEncoding = Encoding.UTF8;

builder.Host
    .ConfigBasicHttpService(opts =>
    {
        var headerOptions = new RestHttpClientHeaderOptions();
        headerOptions.AddForwardedHeader("X-Trace-Event-Id");
        headerOptions.AddForwardedHeader("Accept-Language");
        opts.ConfigHeaders(headerOptions);
    });

builder.Services.AddOptions();
builder.Services.AddHttpContextAccessor();
builder.Services.AddLogging();
builder.Services.Configure<ServiceUriOptions>(x => x.TestServiceUri = "https://jsonplaceholder.typicode.com");

builder.Services
    .AddHttpClient<ITestService, TestService>((serviceProvider, client) =>
    {
        var baseUri = serviceProvider.GetRequiredService<IOptions<ServiceUriOptions>>().Value.TestServiceUri;
        client.BaseAddress = new(baseUri);
    })
    .AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNameCaseInsensitive = true);

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

app.UseRouting();
app.MapControllers();

await app.RunAsync();

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(2, TimeSpan.FromSeconds(30));
}
