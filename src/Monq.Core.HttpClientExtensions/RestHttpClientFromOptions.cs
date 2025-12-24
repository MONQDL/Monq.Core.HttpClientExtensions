using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net.Http;

namespace Monq.Core.HttpClientExtensions
{
    /// <summary>
    /// Basic type of Http service that has a single access point in the form of BaseUri.
    /// </summary>
    public class RestHttpClientFromOptions<TOptions> : RestHttpClient
        where TOptions : class, new()
    {
        /// <summary>
        /// Base Uri Http service.
        /// </summary>
        protected string BaseUri { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestHttpClientFromOptions{TOptions}"/> class.
        /// </summary>
        /// <param name="httpClient">The HttpClient from http client factory.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="baseUri">The base Uri of all requests. For example: http://rsm.api.monq.cloud</param>
        public RestHttpClientFromOptions(
            HttpClient httpClient,
            ILoggerFactory loggerFactory,
            RestHttpClientOptions configuration,
            IHttpContextAccessor httpContextAccessor,
            string baseUri) : base(httpClient, loggerFactory, configuration, httpContextAccessor)
        {
            if (string.IsNullOrWhiteSpace(baseUri))
                throw new ArgumentNullException(nameof(baseUri), "The base uri not set.");
            BaseUri = AddTrailingSlash(baseUri);

            HttpClient.BaseAddress = new Uri(BaseUri);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestHttpClientFromOptions{TOptions}"/> class.
        /// </summary>
        /// <param name="optionsAccessor">The options.</param>
        /// <param name="httpClient">The HttpClient from http client factory.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="baseUri">The base Uri of all requests. For example: http://rsm.api.monq.cloud</param>
        public RestHttpClientFromOptions(
            IOptions<TOptions> optionsAccessor,
            HttpClient httpClient,
            ILoggerFactory loggerFactory,
            RestHttpClientOptions configuration,
            IHttpContextAccessor httpContextAccessor,
            string baseUri) : this(httpClient, loggerFactory, configuration, httpContextAccessor, baseUri)
        {

        }

        static string AddTrailingSlash(string baseUri)
        {
            if (baseUri.Last() != '/')
                return baseUri + "/";

            return baseUri;
        }
    }
}
