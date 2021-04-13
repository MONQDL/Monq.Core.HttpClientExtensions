using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using IdentityModel.Client;

namespace Monq.Core.HttpClientExtensions.Services
{
    /// <summary>
    /// Базовый тип Http сервиса.
    /// </summary>
    public abstract class BasicHttpService
    {
        const string BearerIdentifier = "Bearer";
        const string AuthorizationHeader = "Authorization";

        /// <summary>
        /// Обработчик сообщений HTTP.
        /// </summary>
        public HttpMessageHandler? HttpMessageInvoker { get; }

        /// <summary>
        /// Логгер.
        /// </summary>
        public ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Encapsulates all HTTP-specific information about an individual HTTP request.
        /// </summary>
        public IHttpContextAccessor? HttpContextAccessor { get; }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public BasicHttpServiceOptions Configuration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicHttpService"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="httpMessageInvoker">The HTTP message invoker.</param>
        protected BasicHttpService(
            ILoggerFactory loggerFactory,
            BasicHttpServiceOptions configuration,
            IHttpContextAccessor? httpContextAccessor,
            HttpMessageHandler? httpMessageInvoker = null
            )
        {
            HttpContextAccessor = httpContextAccessor;
            LoggerFactory = loggerFactory;
            HttpMessageInvoker = httpMessageInvoker;
            Configuration = configuration;
        }

        /// <summary>
        /// Создать новый экземпляр класса <see cref="RestHttpClient"/>.
        /// </summary>
        public virtual RestHttpClient CreateRestHttpClient()
        {
            var restHttpClient = HttpMessageInvoker is not null ? new RestHttpClient(this, HttpMessageInvoker) : new RestHttpClient(this);

            if (HttpContextAccessor?.HttpContext is null
                || !HttpContextAccessor.HttpContext.Request.Headers.TryGetValue(AuthorizationHeader, out var authorizeHeader)
                || string.IsNullOrEmpty(authorizeHeader))
                return restHttpClient;

            var token = authorizeHeader.FirstOrDefault();
            if (token is null || !token.StartsWith(BearerIdentifier, StringComparison.OrdinalIgnoreCase))
                return restHttpClient;

            token = token.Replace(BearerIdentifier, string.Empty).TrimStart();
            restHttpClient.SetBearerToken(token);
            return restHttpClient;
        }
    }
}