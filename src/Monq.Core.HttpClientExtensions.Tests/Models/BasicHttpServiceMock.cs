using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monq.Core.HttpClientExtensions.Services;
using System.Net.Http;

namespace Monq.Core.HttpClientExtensions.Tests.Models
{
    public class BasicHttpServiceMock : BasicHttpService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BasicHttpServiceMock"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="httpMessageInvoker">The HTTP message invoker.</param>
        public BasicHttpServiceMock(
            ILoggerFactory loggerFactory,
            BasicHttpServiceOptions configuration,
            IHttpContextAccessor httpContextAccessor,
            HttpMessageHandler? httpMessageInvoker = null)
            : base(loggerFactory, configuration, httpContextAccessor, httpMessageInvoker)
        {
        }
    }

    public class BasicSingleHttpServiceMock : BasicSingleHttpService<ServiceOptions>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BasicSingleHttpServiceMock"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="httpMessageInvoker">The HTTP message invoker.</param>
        public BasicSingleHttpServiceMock(
            IOptions<ServiceOptions> optionsAccessor,
            ILoggerFactory loggerFactory,
            BasicHttpServiceOptions configuration,
            IHttpContextAccessor httpContextAccessor,
            HttpMessageHandler? httpMessageInvoker = null)
            : base(optionsAccessor, loggerFactory, configuration, httpContextAccessor, optionsAccessor.Value.ServiceUri, httpMessageInvoker)
        {
        }
    }
}
