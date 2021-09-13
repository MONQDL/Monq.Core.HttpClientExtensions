using IdentityModel.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Monq.Core.HttpClientExtensions.Exceptions;
using Monq.Core.HttpClientExtensions.Tests.Models;
using Monq.Core.HttpClientExtensions.Tests.Stubs;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Monq.Core.HttpClientExtensions.Tests
{
    public class RestHttpClientTests
    {
        static readonly CamelCasePropertyNamesContractResolver _jsonResolver = new CamelCasePropertyNamesContractResolver { NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = true } };

        const string TraceEventIdHeader = "X-Trace-Event-Id";
        const string UserspaceIdHeader = "X-Smon-Userspace-Id";

        readonly IList<StubLogger> _loggers = new List<StubLogger>();
        readonly ILoggerFactory _loggerFactory;
        readonly BasicHttpServiceOptions _configuration = new BasicHttpServiceOptions();

        public RestHttpClientTests()
        {
            _loggerFactory = new StubLoggerFactory(_loggers);
            RestHttpClient.ResetAccessToken();
            RestHttpClient.ResetAuthorizationRequestHandler();

            _configuration.ConfigHeaders(new BasicHttpServiceHeaderOptions()
            {
                ForwardedHeaders = new HashSet<string>() { TraceEventIdHeader, UserspaceIdHeader }
            });
        }

        [Fact(DisplayName = "Выполнить GET запрос по абсолютному URI.")]
        public async Task ShouldProperlyMakeGetRequestByAbsoluteUri()
        {
            var modelJson = JsonConvert.SerializeObject(new Service[] {
                    new Service { Id = 1, Name = "Service1" },
                    new Service { Id = 2, Name = "Service2" }
                });

            const string uri = "http://unittest/api/services";

            var client = new HttpClient(CreateDefaultResponseHandler(HttpStatusCode.OK, modelJson));
            var restHttpClient = CreateRestHttpClient(client);

            var response = await restHttpClient.Get<IEnumerable<Service>>(uri, TimeSpan.FromSeconds(10));

            var services = response.ResultObject?.ToList();

            Assert.NotNull(services);
            Assert.Equal(2, services!.Count);
            Assert.Equal(1, services[0].Id);
            Assert.Equal("Service1", services[0].Name);
            Assert.Equal(2, services[1].Id);
            Assert.Equal("Service2", services[1].Name);

            var logger = _loggers.First();

            Assert.Equal(2, logger.LoggingEvents.Count);
            Assert.Equal($"Start downstream request GET {uri} with http headers=.", logger.LoggingEvents[0]);
            Assert.Contains($"Downstream request GET {uri} with http headers= finished with StatusCode {(int)HttpStatusCode.OK} at", logger.LoggingEvents[1]);
        }

        [Fact(DisplayName = "Выполнить GET запрос по абсолютному URI с пробросом заголовков.")]
        public async Task ShouldProperlyMakeGetRequestByAbsoluteUriWithForwardedHeaders()
        {
            var modelJson = JsonConvert.SerializeObject(new Service[] {
                    new Service { Id = 1, Name = "Service1" },
                    new Service { Id = 2, Name = "Service2" }
                });
            var traceEventId = Guid.NewGuid().ToString();
            const string userspaceId = "10";
            const string uri = "http://unittest/api/services";

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Add(TraceEventIdHeader, traceEventId);
            httpContext.Request.Headers.Add(UserspaceIdHeader, userspaceId);

            var headerOptions = new BasicHttpServiceHeaderOptions();
            headerOptions.AddForwardedHeader(TraceEventIdHeader);
            headerOptions.AddForwardedHeader(UserspaceIdHeader);
            var conf = new BasicHttpServiceOptions();
            conf.ConfigHeaders(headerOptions);

            var client = new HttpClient(CreateDefaultResponseHandler(HttpStatusCode.OK, modelJson));
            var restHttpClient = CreateRestHttpClient(client, httpContext, conf);

            var response = await restHttpClient.Get<IEnumerable<Service>>(uri, TimeSpan.FromSeconds(10));

            Assert.True(client.DefaultRequestHeaders.Contains(TraceEventIdHeader));
            Assert.True(client.DefaultRequestHeaders.Contains(UserspaceIdHeader));

            Assert.Equal(traceEventId, client.DefaultRequestHeaders.FirstOrDefault(x => x.Key == TraceEventIdHeader).Value.FirstOrDefault());
            Assert.Equal(userspaceId, client.DefaultRequestHeaders.FirstOrDefault(x => x.Key == UserspaceIdHeader).Value.FirstOrDefault());

            var services = response.ResultObject?.ToList();

            Assert.NotNull(services);
            Assert.Equal(2, services!.Count);
            Assert.Equal(1, services[0].Id);
            Assert.Equal("Service1", services[0].Name);
            Assert.Equal(2, services[1].Id);
            Assert.Equal("Service2", services[1].Name);
            var _logger = _loggers.First();
            Assert.Equal(2, _logger.LoggingEvents.Count);
            Assert.Equal($"Start downstream request GET {uri} with http headers=[{TraceEventIdHeader}, {traceEventId}], [{UserspaceIdHeader}, {userspaceId}].", _logger.LoggingEvents[0]);
            Assert.Contains($"Downstream request GET {uri} with http headers=[{TraceEventIdHeader}, {traceEventId}], [{UserspaceIdHeader}, {userspaceId}] finished with StatusCode {(int)HttpStatusCode.OK} at", _logger.LoggingEvents[1]);
        }

        [Fact(DisplayName = "Выполнить GET запрос по относительному URI.")]
        public async Task ShouldProperlyMakeGetRequestByRelativeUri()
        {
            var modelJson = JsonConvert.SerializeObject(new Service[] {
                    new Service { Id = 1, Name = "Service1" },
                    new Service { Id = 2, Name = "Service2" }
                });

            var baseUri = new Uri("http://unittest/");
            const string uri = "api/services";
            var fullUri = new Uri(baseUri, uri);

            var client = new HttpClient(CreateDefaultResponseHandler(HttpStatusCode.OK, modelJson));
            var restHttpClient = CreateRestHttpClient(client);

            client.BaseAddress = baseUri;
            var response = await restHttpClient.Get<IEnumerable<Service>>(uri, TimeSpan.FromSeconds(10));

            var services = response.ResultObject?.ToList();

            Assert.NotNull(services);
            Assert.Equal(2, services!.Count);
            Assert.Equal(1, services[0].Id);
            Assert.Equal("Service1", services[0].Name);
            Assert.Equal(2, services[1].Id);
            Assert.Equal("Service2", services[1].Name);
            var _logger = _loggers.First();
            Assert.Equal(2, _logger.LoggingEvents.Count);
            Assert.Equal($"Start downstream request GET {fullUri} with http headers=.", _logger.LoggingEvents[0]);
            Assert.Contains($"Downstream request GET {fullUri} with http headers= finished with StatusCode {(int)HttpStatusCode.OK} at", _logger.LoggingEvents[1]);
        }

        [Fact(DisplayName = "Исключение на провалившемся GET запросе.")]
        public void ShouldProperlyThrowResponseExceptionOnFailedGetRequest()
        {
            var modelJson = JsonConvert.SerializeObject(new { message = "Ошибка в запросе." });

            const string uri = "http://unittest/api/services";

            var client = new HttpClient(CreateDefaultResponseHandler(HttpStatusCode.BadRequest, modelJson));
            var restHttpClient = CreateRestHttpClient(client);

            var ex = Assert.Throws<ResponseException>(() => restHttpClient.Get<IEnumerable<Service>>(uri, TimeSpan.FromSeconds(10)).GetAwaiter().GetResult());

            Assert.NotNull(ex);
            Assert.Equal(modelJson, ex.ResponseData);
            Assert.Equal((int)HttpStatusCode.BadRequest, (int)ex.StatusCode);
            Assert.Contains($"Downstream request failed with status code {(int)HttpStatusCode.BadRequest} at", ex.Message);
            Assert.Contains($"Response body: {modelJson}.", ex.Message);

            var _logger = _loggers.First();
            Assert.Equal($"Start downstream request GET {uri} with http headers=.", _logger.LoggingEvents[0]);
            Assert.Contains($"Downstream request GET {uri} with http headers= failed with StatusCode {(int)HttpStatusCode.BadRequest} at", _logger.LoggingEvents[1]);
            Assert.Contains($"Request body: (null). Response body: {modelJson}.", _logger.LoggingEvents[1]);
        }

        [Fact(DisplayName = "Пробросить текст исключения из нижестоящего сервиса на провалившемся GET запросе.")]
        public void ShouldProperlyRethrowExceptionOnFailedGetRequest()
        {
            const string uri = "http://unittest/api/services";

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Throws(new HttpRequestException("Request exception."));

            var client = new HttpClient(mockHttpMessageHandler.Object);
            var restHttpClient = CreateRestHttpClient(client);

            Exception ex = Assert.Throws<HttpRequestException>(() => restHttpClient.Get<IEnumerable<Service>>(uri, TimeSpan.FromSeconds(10)).GetAwaiter().GetResult());

            Assert.NotNull(ex);
            Assert.Equal($"Request exception.", ex.Message);
            var _logger = _loggers.First();
            Assert.Equal($"Start downstream request GET {uri} with http headers=.", _logger.LoggingEvents[0]);
            Assert.Contains($"Downstream request GET {uri} with http headers= failed with Exception at", _logger.LoggingEvents[1]);
            Assert.Contains("Request body: (null). Response body: . Exception message: Request exception.", _logger.LoggingEvents[1]);
        }

        [Fact(DisplayName = "Выбросить исключение на провалившемся POST запросе.")]
        public void ShouldProperlyThrowResponseExceptionOnFailedPostRequest()
        {
            var requestModel = new Service() { Id = 12, Name = "Service1" };
            var requestJson = JsonConvert.SerializeObject(requestModel, new JsonSerializerSettings() { ContractResolver = _jsonResolver });
            var modelJson = JsonConvert.SerializeObject(new { message = "Серверная ошибка." });

            const string uri = "http://unittest/api/services";

            var client = new HttpClient(CreateDefaultResponseHandler(HttpStatusCode.InternalServerError, modelJson));
            var restHttpClient = CreateRestHttpClient(client);

            var ex = Assert.Throws<ResponseException>(() => restHttpClient.Post(uri, requestModel, TimeSpan.FromSeconds(10)).GetAwaiter().GetResult());

            Assert.NotNull(ex);
            Assert.Equal(modelJson, ex.ResponseData);
            Assert.Equal((int)HttpStatusCode.InternalServerError, (int)ex.StatusCode);
            Assert.Contains($"Downstream request failed with status code {(int)HttpStatusCode.InternalServerError} at", ex.Message);
            Assert.Contains($"Request body: {requestJson}. Response body: {modelJson}.", ex.Message);

            var _logger = _loggers.First();
            Assert.Equal($"Start downstream request POST {uri} with http headers=.", _logger.LoggingEvents[0]);
            Assert.Contains($"Downstream request POST {uri} with http headers= failed with StatusCode {(int)HttpStatusCode.InternalServerError} at", _logger.LoggingEvents[1]);
            Assert.Contains($"Request body: {requestJson}. Response body: {modelJson}.", _logger.LoggingEvents[1]);
        }

        [Fact(DisplayName = "Проверка правильности установки AccessToken.")]
        public async Task ShouldProperlyGetAccessTokenIfAuthorizationRequestSet()
        {
            var modelJson = JsonConvert.SerializeObject(new Service[] {
                    new Service { Id = 1, Name = "Service1" },
                    new Service { Id = 2, Name = "Service2" }
                });

            const string uri = "http://unittest/api/services";

            var client = new HttpClient(CreateDefaultResponseHandler(HttpStatusCode.OK, modelJson));
            var restHttpClient = CreateRestHttpClient(client);

            var accessTokenRequestCounter = 0;
            RestHttpClient.AuthorizationRequest += async _ =>
            {
                accessTokenRequestCounter++;

                var tokenResponse = new TokenResponse();
                var json = System.Text.Json.JsonDocument.Parse("{ \"access_token\": \"eyJra\", \"refresh_token\": null, \"token_type\": \"Bearer\", \"expires_in\": 3600 }");
                typeof(TokenResponse).GetProperty(nameof(TokenResponse.Json))?.SetValue(tokenResponse, json.RootElement);
                return await Task.FromResult(tokenResponse);
            };

            var response = await restHttpClient.Get<IEnumerable<Service>>(uri, TimeSpan.FromSeconds(10));

            Assert.Equal("eyJra", RestHttpClient.AccessToken?.AccessToken);
            var services = response.ResultObject?.ToList();

            Assert.NotNull(services);
            Assert.Equal(2, services!.Count);
            Assert.Equal(1, services[0].Id);
            Assert.Equal("Service1", services[0].Name);
            Assert.Equal(2, services[1].Id);
            Assert.Equal("Service2", services[1].Name);

            var _logger = _loggers.First();
            Assert.Equal(4, _logger.LoggingEvents.Count);
            Assert.Equal($"Start downstream request GET {uri} with http headers=.", _logger.LoggingEvents[0]);
            Assert.Equal($"Requesting authentication token.", _logger.LoggingEvents[1]);
            Assert.Contains("Authentication token request finished at ", _logger.LoggingEvents[2]);
            Assert.Contains($"Downstream request GET {uri} with http headers= finished with StatusCode {(int)HttpStatusCode.OK} at", _logger.LoggingEvents[3]);

            Assert.Equal(1, accessTokenRequestCounter);
        }

        [Fact(DisplayName = "Проверка переустановки AccessToken при 401 ответе.")]
        public async Task ShouldProperlyRequestAccessTokenIf401Response()
        {
            var modelJson = JsonConvert.SerializeObject(new Service[] {
                    new Service { Id = 1, Name = "Service1" },
                    new Service { Id = 2, Name = "Service2" }
                });

            const string uri = "http://unittest/api/services";

            var client = new HttpClient(CreateDefaultResponseHandler(HttpStatusCode.Unauthorized, modelJson));
            var restHttpClient = CreateRestHttpClient(client);

            var accessTokenRequestCounter = 0;
            RestHttpClient.AuthorizationRequest += async _ =>
            {
                accessTokenRequestCounter++;
                var tokenResponse = new TokenResponse();
                var json = System.Text.Json.JsonDocument.Parse($"{{\"access_token\": \"{(accessTokenRequestCounter == 2 ? "trsJ34" : "eyJra")}\", \"refresh_token\": null, \"token_type\": \"Bearer\", \"expires_in\": 3600 }}");
                typeof(TokenResponse).GetProperty(nameof(TokenResponse.Json))?.SetValue(tokenResponse, json.RootElement);
                return await Task.FromResult(tokenResponse);
            };

            await Assert.ThrowsAsync<ResponseException>(async () => await restHttpClient.Get<IEnumerable<Service>>(uri, TimeSpan.FromSeconds(10)));

            var _logger = _loggers.First();
            Assert.Equal($"Start downstream request GET {uri} with http headers=.", _logger.LoggingEvents[0]);
            Assert.Equal($"Requesting authentication token.", _logger.LoggingEvents[1]);
            Assert.Contains("Authentication token request finished at ", _logger.LoggingEvents[2]);
            Assert.Equal($"Requesting authentication token.", _logger.LoggingEvents[3]);
            Assert.Contains("Authentication token request finished at ", _logger.LoggingEvents[4]);
            Assert.Contains($"Downstream request GET {uri} with http headers= failed with StatusCode {(int)HttpStatusCode.Unauthorized} at", _logger.LoggingEvents[5]);
            Assert.Contains($"Request body: (null). Response body: {modelJson}.", _logger.LoggingEvents[5]);

            Assert.Equal("trsJ34", RestHttpClient.AccessToken?.AccessToken);


            Assert.Equal(2, accessTokenRequestCounter);
        }

        [Fact(DisplayName = "Выполнить сериализацию данных в теле запроса в формает CamelCase.")]
        public async Task ShouldProperlySerializeJsonAsCamelCase()
        {
            var model = new Service() { Id = 12, Name = "Service1" };

            const string uri = "http://unittest/api/services";

            var client = new HttpClient(CreateDefaultResponseHandler(HttpStatusCode.OK, JsonConvert.SerializeObject(model)));
            var restHttpClient = CreateRestHttpClient(client);

            var result = await restHttpClient.Post<Service, Service>(uri, model);
            var serializedRequestBody = await result.OriginalResponse.RequestMessage!.Content!.ReadAsStringAsync();

            Assert.Equal("{\"id\":12,\"name\":\"Service1\"}", serializedRequestBody);

        }

        [Fact(DisplayName = "Повторное использование экземпляра HttpClient.")]
        public async Task ShouldProperlyReuseHttpClientObject()
        {
            var modelJson = JsonConvert.SerializeObject(new Service[] {
                    new Service { Id = 1, Name = "Service1" },
                    new Service { Id = 2, Name = "Service2" }
                });

            const string uri = "http://unittest/api/services";

            var client = new HttpClient(CreateDefaultResponseHandler(HttpStatusCode.OK, modelJson));
            var restHttpClient = CreateRestHttpClient(client);

            var response = await restHttpClient.Get<IEnumerable<Service>>(uri, TimeSpan.FromSeconds(10));

            Assert.Equal(2, response.ResultObject?.Count());

            // Повторный запрос.
            response = await restHttpClient.Get<IEnumerable<Service>>(uri, TimeSpan.FromSeconds(10));
            Assert.Equal(2, response.ResultObject?.Count());

        }

        [Fact(DisplayName = "Повторное использование экземпляра HttpClient в параллельной обработке, с пробросом заголовков.")]
        public async Task ShouldProperlyReuseHttpClientObjectInParallelWithHeaders()
        {
            var modelJson = JsonConvert.SerializeObject(new Service[] {
                    new Service { Id = 1, Name = "Service1" },
                    new Service { Id = 2, Name = "Service2" }
                });

            const string uri = "http://unittest/api/services";

            var headerOptions = new BasicHttpServiceHeaderOptions();
            headerOptions.AddForwardedHeader(TraceEventIdHeader);
            headerOptions.AddForwardedHeader(UserspaceIdHeader);
            var conf = new BasicHttpServiceOptions();
            conf.ConfigHeaders(headerOptions);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Add(TraceEventIdHeader, "eventId");
            httpContext.Request.Headers.Add(UserspaceIdHeader, "12");
            const int totalRequests = 1000;

            var client = new HttpClient(CreateDefaultResponseHandler(HttpStatusCode.OK, modelJson));
            var restHttpClient = CreateRestHttpClient(client, httpContext, conf);

            var firstResult = await restHttpClient.Get<IEnumerable<Service>>(uri, TimeSpan.FromSeconds(10));
            Assert.Equal(2, firstResult.ResultObject?.Count());

            var results = Enumerable.Range(0, totalRequests).AsParallel().AsOrdered().WithDegreeOfParallelism(10)
                .Select(_ => restHttpClient.Get<IEnumerable<Service>>(uri, TimeSpan.FromSeconds(10)).GetAwaiter().GetResult())
                .AsSequential()
                .ToList();
            Assert.Empty(results.Where(x => x.OriginalResponse?.RequestMessage?.Headers.Count() != 2));
            Assert.Equal(2 * totalRequests, results.Where(x => x.ResultObject is not null).SelectMany(x => x.ResultObject).Count());
        }

        RestHttpClientMock CreateRestHttpClient(HttpClient httpClient,
            HttpContext? httpContext = null,
            BasicHttpServiceOptions? configuration = null)
        {
            return new RestHttpClientMock(
                       httpClient,
                       _loggerFactory,
                       configuration ?? _configuration,
                       new HttpContextAccessorStub(httpContext ?? new DefaultHttpContext()));
        }

        HttpMessageHandler CreateDefaultResponseHandler(HttpStatusCode responseCode, string jsonResponseBody)
        {
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = responseCode,
                    Content = new StringContent(jsonResponseBody),
                });

            return mockHttpMessageHandler.Object;
        }
    }
}
