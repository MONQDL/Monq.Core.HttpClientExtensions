using Microsoft.AspNetCore.Http;

namespace Monq.Core.HttpClientExtensions.Tests.Stubs
{
    public class HttpContextAccessorStub : IHttpContextAccessor
    {
        public HttpContextAccessorStub(HttpContext defaultHttpContext)
        {
            HttpContext = defaultHttpContext;
        }

        public HttpContext HttpContext { get; set; }
    }
}
