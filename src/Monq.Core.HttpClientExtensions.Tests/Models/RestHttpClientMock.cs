using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace Monq.Core.HttpClientExtensions.Tests.Models;

public class RestHttpClientMock : RestHttpClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestHttpClientMock"/> class.
    /// </summary>
    /// <param name="httpClient">The HttpClient from http client factory.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public RestHttpClientMock(HttpClient httpClient,
        ILoggerFactory loggerFactory,
        RestHttpClientOptions configuration,
        IHttpContextAccessor httpContextAccessor)
        : base(httpClient, loggerFactory, configuration, httpContextAccessor)
    {

    }
}
