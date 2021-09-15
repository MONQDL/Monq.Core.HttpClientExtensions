using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monq.Core.HttpClientExtensions.Tests.Models;
using Monq.Core.HttpClientExtensions.Tests.Stubs;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Monq.Core.HttpClientExtensions.Tests
{
    public class RestHttpClientFromOptionsTests
    {
        readonly Mock<IOptions<ServiceOptions>> _optionsMoq;

        readonly IList<Service> _testCollection = new List<Service> {
                    new Service { Id = 1, Name = "Service1" },
                    new Service { Id = 2, Name = "Service2" }
                };

        readonly ILoggerFactory _loggerFactory = new StubLoggerFactory(new List<StubLogger>());
        readonly BasicHttpServiceOptions _configuration = new BasicHttpServiceOptions();

        public RestHttpClientFromOptionsTests()
        {
            _optionsMoq = new Mock<IOptions<ServiceOptions>>();
            _optionsMoq
                .Setup(x => x.Value)
                .Returns(new ServiceOptions()
                {
                    ServiceUri = "http://unittest"
                });
        }

        [Fact(DisplayName = "Проверка установки Bearer token из HttpContextAccessor.")]
        public void ShouldProperlySetBearerTokenFromRequest()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Add("Authorization", "Bearer token355");

            var client = new HttpClient(CreateDefaultResponseHandler(HttpStatusCode.Unauthorized));

            var httpService = CreateBasicHttpService(client, httpContext);

            Assert.Equal("token355", client.DefaultRequestHeaders?.Authorization?.Parameter);
            Assert.Equal("Bearer", client.DefaultRequestHeaders?.Authorization?.Scheme);
        }

        [Fact(DisplayName = "Проверка НЕустановки Bearer token из HttpContextAccessor.")]
        public void ShouldNotSetBearerTokenFromRequestIfBearerNotValid()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Add("Authorization", "Bearetoken355");
            var client = new HttpClient(CreateDefaultResponseHandler(HttpStatusCode.Unauthorized));

            var httpService = CreateBasicHttpService(client, httpContext);
            Assert.Null(client.DefaultRequestHeaders.Authorization);
        }

        [Fact(DisplayName = "Добавить слэш в BaseUri, если требуется.")]
        public void ShouldAddTrailingSlashToBaseUri()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Add("Authorization", "Bearetoken355");

            _optionsMoq
                .Setup(x => x.Value)
                .Returns(new ServiceOptions()
                {
                    ServiceUri = "http://unittest"
                });

            var client = new HttpClient(CreateDefaultResponseHandler(HttpStatusCode.Unauthorized));

            var httpService = CreateRestHttpClientFromOptions(client, httpContext, _optionsMoq.Object);
            Assert.Equal(new Uri("http://unittest/"), client.BaseAddress);
        }

        [Fact(DisplayName = "Выполнить запрос по абсолютному Uri.")]
        public async Task ShouldProperlyMakeRequestByAbsoluteUri()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Add("Authorization", "Bearetoken355");

            _optionsMoq
                .Setup(x => x.Value)
                .Returns(new ServiceOptions()
                {
                    ServiceUri = "http://unittest"
                });

            var client = new HttpClient(CreateDefaultResponseHandler(HttpStatusCode.OK));

            var httpService = CreateRestHttpClientFromOptions(client, httpContext, _optionsMoq.Object);
            var result = await httpService.Get<IList<Service>>("http://unittest/api/services");

            Assert.Equal(result.ResultObject, _testCollection);
        }

        [Fact(DisplayName = "Выполнить запрос по относительному Uri.")]
        public async Task ShouldProperlyMakeRequestByRelativeUri()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Add("Authorization", "Bearetoken355");

            _optionsMoq
                .Setup(x => x.Value)
                .Returns(new ServiceOptions()
                {
                    ServiceUri = "http://unittest"
                });

            var client = new HttpClient(CreateDefaultResponseHandler(HttpStatusCode.OK));
            var httpService = CreateRestHttpClientFromOptions(client, httpContext, _optionsMoq.Object);
            var result = await httpService.Get<IList<Service>>("api/services", TimeSpan.FromSeconds(5));

            Assert.Equal(result.ResultObject, _testCollection);
        }

        RestHttpClientMock CreateBasicHttpService(HttpClient httpClient, HttpContext? httpContext)
        {
            return new RestHttpClientMock(
                        httpClient,
                       _loggerFactory,
                       _configuration,
                       new HttpContextAccessorStub(httpContext ?? new DefaultHttpContext()));
        }

        RestHttpClientFromOptionsMock CreateRestHttpClientFromOptions(HttpClient httpClient, HttpContext? httpContext, IOptions<ServiceOptions> optionsAccessor)
        {
            return new RestHttpClientFromOptionsMock(
                       optionsAccessor ?? _optionsMoq.Object,
                       httpClient,
                       _loggerFactory,
                       _configuration,
                       new HttpContextAccessorStub(httpContext ?? new DefaultHttpContext()),
                       optionsAccessor.Value.ServiceUri);
        }

        HttpMessageHandler CreateDefaultResponseHandler(HttpStatusCode responseCode)
        {
            var modelJson = JsonConvert.SerializeObject(_testCollection);

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = responseCode,
                    Content = new StringContent(modelJson),
                });

            return mockHttpMessageHandler.Object;
        }
    }
}