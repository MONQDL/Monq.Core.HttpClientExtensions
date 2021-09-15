using IdentityModel.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Monq.Core.HttpClientExtensions.Exceptions;
using Monq.Core.HttpClientExtensions.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Monq.Core.HttpClientExtensions
{
    /// <summary>
    /// Стек обработчика HTTP-данных,
    /// используемый для отправки запросов с расширенным использованием логирования и набором упрощённых правил запроса.
    /// </summary>
    /// <seealso cref="HttpClient" />
    public class RestHttpClient
    {
        static readonly object LockObj = new object();

        /// <summary>
        /// Семафор для синхронизации потоков получения AccessToken.
        /// </summary>
        static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(10);

        readonly ILogger<RestHttpClient> _log;

        readonly HttpClient _httpClient;

        static DateTime ExpiryTime { get; set; }

        /// <summary>
        /// The default timeout the request will be set if user not specified.
        /// </summary>
        public TimeSpan DefaultTimeout => _defaultTimeout;

        /// <summary>
        /// The actual http client from the IHttpClientFactory.
        /// </summary>
        public HttpClient HttpClient => _httpClient;

        /// <summary>
        /// Access Token, который будет использоваться при Http запросах.
        /// </summary>
        public static TokenResponse? AccessToken { get; private set; }

        /// <summary>
        /// Обработчик события для получения AccessToken.
        /// </summary>
        public delegate Task<TokenResponse> AuthorizationRequestHandler(HttpClient client);

        /// <summary>
        /// При вызове данного события требуется получить AccessToken от Identity сервера.
        /// </summary>
        public static event AuthorizationRequestHandler? AuthorizationRequest;

        const string BearerIdentifier = "Bearer";
        const string AuthorizationHeader = "Authorization";

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
        public RestHttpClientOptions Configuration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestHttpClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HttpClient from http client factory.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        public RestHttpClient(HttpClient httpClient,
            ILoggerFactory loggerFactory,
            RestHttpClientOptions configuration,
            IHttpContextAccessor? httpContextAccessor)
        {
            _httpClient = httpClient;

            HttpContextAccessor = httpContextAccessor;
            LoggerFactory = loggerFactory;
            Configuration = configuration;

            _log = loggerFactory.CreateLogger<RestHttpClient>();

            // To reuse the HttpClient instance, we will use the cancellation token to manage timeouts.
            // To do this, you need to set the main timeout to the maximum value,
            // because it will override the value specified in the cancellation token.
            _httpClient.Timeout = System.Threading.Timeout.InfiniteTimeSpan;

            if (HttpContextAccessor?.HttpContext is not null
                && HttpContextAccessor.HttpContext.Request.Headers.TryGetValue(AuthorizationHeader, out var authorizeHeader)
                && !string.IsNullOrEmpty(authorizeHeader))
            {
                var token = authorizeHeader.FirstOrDefault();
                if (token is not null && token.StartsWith(BearerIdentifier, StringComparison.OrdinalIgnoreCase))
                {
                    token = token.Replace(BearerIdentifier, string.Empty).TrimStart();
                    httpClient.SetBearerToken(token);
                }
            }
        }

        /// <summary>
        /// Выполнить HTTP DELETE запрос, и вернуть результат десериализованный в тип <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TResult">Тип результата.</typeparam>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ResponseException"></exception>
        public Task<RestHttpResponseMessage<TResult?>> Delete<TResult>(string uri, CancellationToken cancellationToken = default) =>
            MakeRequestWithoutBody<TResult?>("DELETE", uri, cancellationToken);

        /// <summary>
        /// Выполнить HTTP DELETE запрос, результат не возвращать.
        /// </summary>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ResponseException"></exception>
        public Task Delete(string uri, CancellationToken cancellationToken = default) =>
           MakeRequestWithoutBody("DELETE", uri, cancellationToken);

        /// <summary>
        /// Выполнить HTTP DELETE запрос c телом типа <typeparamref name="TRequest"/>, результат не возвращать.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ResponseException"></exception>
        public Task Delete<TRequest>(string uri, TRequest value, CancellationToken cancellationToken = default) =>
            MakeRequestWithBody<TRequest, object>("DELETE", uri, value, cancellationToken);

        /// <summary>
        /// Выполнить HTTP Get запрос и вернуть результат десериализованный в тип <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TResult">Тип результата запроса.</typeparam>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ResponseException"></exception>
        public Task<RestHttpResponseMessage<TResult?>> Get<TResult>(string uri, CancellationToken cancellationToken = default) =>
            MakeRequestWithoutBody<TResult>("GET", uri, cancellationToken);

        /// <summary>
        /// Выполнить HTTP PATCH запрос c телом типа <typeparamref name="TRequest"/>, результат не возвращать.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ResponseException"></exception>
        public Task Patch<TRequest>(string uri, TRequest value, CancellationToken cancellationToken = default) =>
            MakeRequestWithBody("PATCH", uri, value, cancellationToken);

        /// <summary>
        /// Выполнить HTTP PATCH запрос c телом типа <typeparamref name="TRequest"/> 
        /// и вернуть результат десериализованный в тип <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <typeparam name="TResult">Тип результата.</typeparam>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ResponseException"></exception>
        public Task<RestHttpResponseMessage<TResult?>> Patch<TRequest, TResult>(string uri,
            TRequest value,
            CancellationToken cancellationToken = default) =>
            MakeRequestWithBody<TRequest, TResult?>("PATCH", uri, value, cancellationToken);

        /// <summary>
        /// Выполнить HTTP POST запрос c телом типа <typeparamref name="TRequest"/> 
        /// и вернуть результат десериализованный в тип <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <typeparam name="TResult">Тип результата.</typeparam>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ResponseException"></exception>
        public Task<RestHttpResponseMessage<TResult?>> Post<TRequest, TResult>(string uri,
            TRequest value,
            CancellationToken cancellationToken = default) =>
            MakeRequestWithBody<TRequest, TResult?>("POST", uri, value, cancellationToken);

        /// <summary>
        /// Выполнить HTTP POST запрос c телом типа <typeparamref name="TRequest"/>, результат не возвращать.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ResponseException"></exception>
        public Task Post<TRequest>(string uri, TRequest value, CancellationToken cancellationToken = default) =>
            MakeRequestWithBody("POST", uri, value, cancellationToken);

        /// <summary>
        /// Выполнить HTTP PUT запрос c телом типа <typeparamref name="TRequest"/> 
        /// и вернуть результат десериализованный в тип <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <typeparam name="TResult">Тип результата.</typeparam>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ResponseException"></exception>
        public Task<RestHttpResponseMessage<TResult?>> Put<TRequest, TResult>(string uri,
            TRequest value,
            CancellationToken cancellationToken = default) =>
            MakeRequestWithBody<TRequest, TResult?>("PUT", uri, value, cancellationToken);

        /// <summary>
        /// Выполнить HTTP PUT запрос c телом типа <typeparamref name="TRequest"/>, результат не возвращать.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ResponseException"></exception>
        public Task Put<TRequest>(string uri, TRequest value, CancellationToken cancellationToken = default) =>
            MakeRequestWithBody("PUT", uri, value, cancellationToken);

        /// <summary>
        /// Get AccessToken using <see cref="AuthorizationRequest"/> handler.
        /// </summary>
        public async Task<TokenResponse?> GetAccessToken(bool invokeHandler)
        {
            var handler = AuthorizationRequest;
            if (handler is null)
                return null;

            // use token if it exists and is still fresh.
            if (AccessToken is not null && ExpiryTime > DateTime.UtcNow && !invokeHandler)
                return AccessToken;

            // Obtain new token.
            // Если одновременно несколько потоков попытаются вызвать метод получения Access токена,
            // то доступ дадим только одному.
            await SemaphoreSlim.WaitAsync();
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                if (AccessToken is not null && ExpiryTime > DateTime.UtcNow && !invokeHandler)
                    return AccessToken;

                _log.LogInformation("Requesting authentication token.");
                var accessTokenResponse = await handler(_httpClient);
                sw.Stop();
                _log.LogInformation("Authentication token request finished at {elapsedMilliseconds} ms.",
                    sw.ElapsedMilliseconds);
                if (accessTokenResponse.IsError)
                {
                    throw new SecurityTokenException("Could not retrieve token.");
                }

                //set Token to the new token and set the expiry time to the new expiry time
                AccessToken = accessTokenResponse;
                ExpiryTime = DateTime.UtcNow.AddSeconds(AccessToken.ExpiresIn);

                //return fresh token
                return AccessToken;
            }
            catch (Exception e)
            {
                _log.LogCritical(e, $"Raised error during authentication token request. Details: {e.Message}");
            }
            finally
            {
                sw.Stop();
                SemaphoreSlim.Release();
            }

            return null;
        }

        /// <summary>
        /// Resets the access token.
        /// </summary>
        public static void ResetAccessToken()
        {
            lock (LockObj)
            {
                AccessToken = null;
            }
        }

        /// <summary>
        /// Удалить всех подписчиков события AuthorizationRequest.
        /// </summary>
        public static void ResetAuthorizationRequestHandler()
        {
            AuthorizationRequest = null;
        }

        /// <summary>
        /// Set the bearer token authentication token.
        /// </summary>
        /// <param name="token">OAuth 2 token.</param>
        public void SetBearerToken(string token)
        {
            _httpClient.SetBearerToken(token);
        }

        Dictionary<string, string> GetForwardedHeaders()
        {
            var headers = new Dictionary<string, string>();
            if (HttpContextAccessor is not null && Configuration.RestHttpClientHeaderOptions.LogForwardedHeaders)
            {
                foreach (var header in Configuration.RestHttpClientHeaderOptions.ForwardedHeaders)
                {
                    var requestHeaderValue = (string?)HttpContextAccessor?.HttpContext?.Request?.Headers[header];
                    if (!string.IsNullOrEmpty(requestHeaderValue))
                        headers.Add(header, requestHeaderValue);
                }
            }

            return headers;
        }

        Uri GetAbsoluteUri(string uri)
        {
            if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                return new Uri(_httpClient.BaseAddress, uri);
            return new Uri(uri);
        }

        void CheckStatusCode(string method,
            Uri uri,
            HttpResponseMessage response,
            string? requestData,
            string? responseData,
            Stopwatch sw)
        {
            if (response.IsSuccessStatusCode)
                return;

            var headers = GetForwardedHeaders();

            _log.LogError(
                new EventId(TraceConstants.DownServiceEventId),
                "Downstream request {Method} {Path} with http headers={HttpForwardedHeaders} failed with " +
                "StatusCode {StatusCode} at {elapsedMilliseconds} ms. Request body: {@ServiceRequestData}. " +
                "Response body: {@ServiceResponseData}.",
                method,
                uri.ToString(),
                headers,
                (int)response.StatusCode,
                sw.ElapsedMilliseconds,
                requestData,
                responseData);
            throw new ResponseException(
                      $"Downstream request failed with status code {(int)response.StatusCode} at {sw.ElapsedMilliseconds} ms. " +
                      $"Request body: {requestData}. Response body: {responseData}.",
                      response.StatusCode,
                      responseData);
        }

        void LogEndEvent(string method, Uri uri, HttpStatusCode statusCode, Stopwatch sw)
        {
            var headers = GetForwardedHeaders();
            _log.LogInformation(
                     new EventId(TraceConstants.DownServiceEventId),
                     "Downstream request {Method} {Path} with http headers={HttpForwardedHeaders} finished with " +
                     "StatusCode {StatusCode} at {elapsedMilliseconds} ms.",
                     method,
                     uri.ToString(),
                     headers,
                     (int)statusCode,
                     sw.ElapsedMilliseconds);
        }

        void LogRequestException(string method, Uri uri, string? requestData, string responseData, Exception e, Stopwatch sw)
        {
            var headers = GetForwardedHeaders();
            _log.LogError(
                new EventId(TraceConstants.DownServiceEventId),
                e,
                "Downstream request {Method} {Path} with http headers={HttpForwardedHeaders} failed with " +
                "Exception at {elapsedMilliseconds} ms. " +
                "Request body: {@ServiceRequestData}. Response body: {@ServiceResponseData}. Exception message: " + e.Message,
                method,
                uri.ToString(),
                headers,
                sw.ElapsedMilliseconds,
                requestData,
                responseData);
        }

        void LogStartEvent(string method, Uri uri)
        {
            var headers = GetForwardedHeaders();
            _log.LogInformation(
                     new EventId(TraceConstants.DownServiceEventId),
                     "Start downstream request {Method} {Path} with http headers={HttpForwardedHeaders}.",
                     method,
                     uri.ToString(),
                     headers);
        }

        void PassThroughForwardedHeaders()
        {
            if (HttpContextAccessor is null)
                return;

            foreach (var header in Configuration.RestHttpClientHeaderOptions.ForwardedHeaders)
            {
                var requestHeaderValue = (string?)HttpContextAccessor.HttpContext?.Request.Headers[header];
                if (string.IsNullOrEmpty(requestHeaderValue)
                    || _httpClient.DefaultRequestHeaders.Contains(header))
                    continue;

                _httpClient.DefaultRequestHeaders.Add(header, requestHeaderValue);
            }
        }

        Task MakeRequestWithBody<TRequest>(string requestType,
            string uri,
            TRequest value,
            CancellationToken? cancellationToken = default) =>
            MakeRequestWithBody<TRequest, object>(requestType, uri, value, cancellationToken);

        [return: NotNull]
        protected virtual async Task<RestHttpResponseMessage<TResult?>> MakeRequestWithBody<TRequest, TResult>(
            string requestType,
            string uri,
            TRequest value,
            CancellationToken? cancellationToken = default)
        {
            var cts = cancellationToken ?? new CancellationTokenSource(DefaultTimeout).Token;

            var sw = new Stopwatch();
            sw.Start();
            // Выполняем проброс указанных заголовков в опциях в нижестоящие сервисы.
            PassThroughForwardedHeaders();

            var fullUri = GetAbsoluteUri(uri);
            LogStartEvent(requestType, fullUri);

            var result = string.Empty;
            HttpResponseMessage response;
            var serializedRequestValue = RestHttpClientSerializer.Serialize(value);
            try
            {
                await SetToken();

                var method = new HttpMethod(requestType);
                var request = new HttpRequestMessage(method, uri)
                {
                    Content = new StringContent(serializedRequestValue, Encoding.UTF8, "application/json")
                };

                response = await _httpClient.SendAsync(request, cts);
                response.RequestMessage = request;

                // Перезапросить токен при ответе 401
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await SetToken(true);
                    // Нельзя послать 2 раза одинаковый запрос.
                    var request2 = new HttpRequestMessage(method, uri);
                    response = await _httpClient.SendAsync(request2, cts);
                    response.RequestMessage = request2;
                }

                var content = response.Content;
                result = await content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                sw.Stop();
                LogRequestException(requestType, fullUri, serializedRequestValue, result, e, sw);
                throw;
            }
            finally
            {
                sw.Stop();
            }

            CheckStatusCode(requestType, fullUri, response, serializedRequestValue, result, sw);
            LogEndEvent(requestType, fullUri, response.StatusCode, sw);

            return new RestHttpResponseMessage<TResult?>(response) { ResultObject = result.JsonToObject<TResult>() };
        }

        async Task MakeRequestWithoutBody(string requestType, string uri, CancellationToken? cancellationToken = default) =>
            await MakeRequestWithoutBody<object>(requestType, uri, cancellationToken);

        [return: NotNull]
        protected virtual async Task<RestHttpResponseMessage<TResult?>> MakeRequestWithoutBody<TResult>(
            string requestType, string uri, CancellationToken? cancellationToken = default)
        {
            var cts = cancellationToken ?? new CancellationTokenSource(DefaultTimeout).Token;

            var sw = new Stopwatch();
            sw.Start();
            // Выполняем проброс указанных заголовков в опциях в нижестоящие сервисы.
            PassThroughForwardedHeaders();

            var fullUri = GetAbsoluteUri(uri);
            LogStartEvent(requestType, fullUri);
            HttpResponseMessage response;

            string result = string.Empty;
            try
            {
                await SetToken();

                var method = new HttpMethod(requestType);

                var request = new HttpRequestMessage(method, uri);
                response = await _httpClient.SendAsync(request, cts);
                response.RequestMessage = request;

                // Перезапросить токен при ответе 401
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await SetToken(true);
                    // Нельзя послать 2 раза одинаковый запрос.
                    var request2 = new HttpRequestMessage(method, uri);
                    response = await _httpClient.SendAsync(request2, cts);
                    response.RequestMessage = request2;
                }

                var content = response.Content;
                result = await content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                sw.Stop();
                LogRequestException(requestType, fullUri, null, result, e, sw);
                throw;
            }
            finally
            {
                sw.Stop();
            }

            CheckStatusCode(requestType, fullUri, response, null, result, sw);
            LogEndEvent(requestType, fullUri, response.StatusCode, sw);

            return new RestHttpResponseMessage<TResult?>(response) { ResultObject = result.JsonToObject<TResult>() };
        }

        async Task SetToken(bool invokeHandler = false)
        {
            // If token was set by HttpAccessHandler use it.
            if (_httpClient.DefaultRequestHeaders?.Authorization?.Parameter is not null && !invokeHandler)
                return;

            var token = await GetAccessToken(invokeHandler);
            if (token != null)
                SetBearerToken(token.AccessToken);
        }
    }
}