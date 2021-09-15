using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monq.Core.HttpClientExtensions.Services;
using System.Net.Http;

namespace Monq.Core.HttpClientExtensions.Tests.Models
{
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
            BasicHttpServiceOptions configuration, 
            IHttpContextAccessor httpContextAccessor) 
            : base(httpClient, loggerFactory, configuration, httpContextAccessor)
        {

        }
    }

    public class RestHttpClientFromOptionsMock : RestHttpClientFromOptions<ServiceOptions>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestHttpClientFromOptionsMock`1"/> class.
        /// </summary>
        /// <param name="optionsAccessor">The options.</param>
        /// <param name="httpClient">The HttpClient from http client factory.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="baseUri">The base Uri of all requests. For example: http://rsm.api.monq.cloud</param>
        public RestHttpClientFromOptionsMock(IOptions<ServiceOptions> optionsAccessor, 
            HttpClient httpClient, 
            ILoggerFactory loggerFactory, 
            BasicHttpServiceOptions configuration, 
            IHttpContextAccessor httpContextAccessor, 
            string baseUri) 
            : base(optionsAccessor, httpClient, loggerFactory, configuration, httpContextAccessor, baseUri)
        {

        }
    }
}
