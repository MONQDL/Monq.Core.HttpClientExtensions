using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monq.Core.HttpClientExtensions.Tests.Models;
using Monq.Core.HttpClientExtensions.Tests.Stubs;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Monq.Core.HttpClientExtensions.Tests
{
    public class BasicHttpServiceTests
    {
        readonly Mock<IOptions<ServiceOptions>> _optionsMoq;

        readonly IList<Service> _testCollection = new List<Service> {
                    new Service { Id = 1, Name = "Service1" },
                    new Service { Id = 2, Name = "Service2" }
                };

        readonly ILoggerFactory _loggerFactory = new StubLoggerFactory(new List<StubLogger>());
        readonly BasicHttpServiceOptions _configuration = new BasicHttpServiceOptions();

        public BasicHttpServiceTests()
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

            var httpService = CreateBasicHttpService(CreateDefaultResponseHandler(HttpStatusCode.Unauthorized), httpContext);
            using (var client = httpService.CreateRestHttpClient())
            {
                Assert.Equal("token355", client.DefaultRequestHeaders?.Authorization?.Parameter);
                Assert.Equal("Bearer", client.DefaultRequestHeaders?.Authorization?.Scheme);
            }
        }

        [Fact(DisplayName = "Проверка НЕустановки Bearer token из HttpContextAccessor.")]
        public void ShouldNotSetBearerTokenFromRequestIfBearerNotValid()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Add("Authorization", "Bearetoken355");

            var httpService = CreateBasicHttpService(CreateDefaultResponseHandler(HttpStatusCode.Unauthorized), httpContext);
            using (var client = httpService.CreateRestHttpClient())
            {
                Assert.Null(client.DefaultRequestHeaders.Authorization);
            }
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

            var httpService = CreateBasicSingleHttpService(CreateDefaultResponseHandler(HttpStatusCode.Unauthorized), httpContext, _optionsMoq.Object);
            using (var client = httpService.CreateRestHttpClient())
            {
                Assert.Equal(new Uri("http://unittest/"), client.BaseAddress);
            }
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

            var httpService = CreateBasicSingleHttpService(CreateDefaultResponseHandler(HttpStatusCode.OK), httpContext, _optionsMoq.Object);
            using (var client = httpService.CreateRestHttpClient())
            {
                var result = await client.Get<IList<Service>>("http://unittest/api/services", TimeSpan.FromSeconds(5));

                Assert.Equal(result.ResultObject, _testCollection);
            }
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

            var httpService = CreateBasicSingleHttpService(CreateDefaultResponseHandler(HttpStatusCode.OK), httpContext, _optionsMoq.Object);
            using (var client = httpService.CreateRestHttpClient())
            {
                var result = await client.Get<IList<Service>>("api/services", TimeSpan.FromSeconds(5));

                Assert.Equal(result.ResultObject, _testCollection);
            }
        }

        BasicHttpServiceMock CreateBasicHttpService(FakeResponseHandler fakeResponseHandler, HttpContext? httpContext)
        {
            return new BasicHttpServiceMock(
                       _loggerFactory,
                       _configuration,
                       new HttpContextAccessorStub(httpContext ?? new DefaultHttpContext()),
                       fakeResponseHandler);
        }

        BasicSingleHttpServiceMock CreateBasicSingleHttpService(FakeResponseHandler fakeResponseHandler, HttpContext? httpContext, IOptions<ServiceOptions> optionsAccessor)
        {
            return new BasicSingleHttpServiceMock(
                       optionsAccessor,
                       _loggerFactory,
                       _configuration,
                       new HttpContextAccessorStub(httpContext ?? new DefaultHttpContext()),
                       fakeResponseHandler);
        }

        FakeResponseHandler CreateDefaultResponseHandler(HttpStatusCode responseCode)
        {
            var modelJson = JsonConvert.SerializeObject(_testCollection);
            var fakeResponseHandler = new FakeResponseHandler();

            const string uri = "http://unittest/api/services";

            fakeResponseHandler.AddFakeResponse(new Uri(uri), new HttpResponseMessage(responseCode), modelJson);
            return fakeResponseHandler;
        }
    }
}