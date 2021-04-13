using System;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Monq.Core.HttpClientExtensions.Services
{
    /// <summary>
    /// Базовый тип Http сервиса, который имеет единую точку доступа в виде BaseUri.
    /// </summary>
    public abstract class BasicSingleHttpService<TOptions> : BasicHttpService
        where TOptions : class, new()

    {
        /// <summary>
        /// Базовый Uri Http сервиса.
        /// </summary>
        protected string BaseUri { get; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="BasicSingleHttpService{TOptions}" />.
        /// </summary>
        /// <param name="optionsAccessor">The options.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="baseUri">Базовый Uri, по которому доступен микросервис. Например: http://rsm.api.smon.monq.ru</param>
        /// <param name="httpMessageInvoker">The HTTP message invoker.</param>
        /// <exception cref="ArgumentNullException">baseUri - Не указан базовый Uri сервиса.</exception>
        protected BasicSingleHttpService(
            IOptions<TOptions> optionsAccessor,
            ILoggerFactory loggerFactory,
            BasicHttpServiceOptions configuration,
            IHttpContextAccessor? httpContextAccessor,
            string baseUri,
            HttpMessageHandler? httpMessageInvoker = null)
            : base(loggerFactory, configuration, httpContextAccessor, httpMessageInvoker)
        {
            if (string.IsNullOrWhiteSpace(baseUri))
                throw new ArgumentNullException(nameof(baseUri), "The base uri not set.");
            BaseUri = AddTrailingSlash(baseUri);
        }

        /// <summary>
        /// Создать новый экземпляр класса <see cref="T:Monq.Core.HttpClientExtensions.RestHttpClient" />.
        /// </summary>
        public override RestHttpClient CreateRestHttpClient()
        {
            var client = base.CreateRestHttpClient();

            client.BaseAddress = new Uri(BaseUri);

            return client;
        }

        string AddTrailingSlash(string baseUri)
        {
            if (baseUri.Last() != '/')
                return baseUri + "/";

            return baseUri;
        }
    }
}
