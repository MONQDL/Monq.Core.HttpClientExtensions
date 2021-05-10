using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Monq.Core.HttpClientExtensions.Exceptions;
using Monq.Core.HttpClientExtensions.Extensions;
using Monq.Core.HttpClientExtensions.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    public class RestHttpClient : HttpClient
    {
        static readonly object LockObj = new object();

        /// <summary>
        /// Семафор для синхронизации потоков получения AccessToken.
        /// </summary>
        static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(10);

        readonly ILogger<RestHttpClient> _log;

        static DateTime ExpiryTime { get; set; }

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

        /// <summary>
        /// Gets the basic HTTP service.
        /// </summary>
        /// <value>
        /// The basic HTTP service.
        /// </value>
        public BasicHttpService BasicHttpService { get; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="RestHttpClient" />.
        /// </summary>
        /// <param name="basicHttpService">The basic HTTP service.</param>
        public RestHttpClient(BasicHttpService basicHttpService)
        {
            BasicHttpService = basicHttpService;
            _log = BasicHttpService.LoggerFactory.CreateLogger<RestHttpClient>();

            // Для переиспользования экземпляра HttpClient будем использовать cancellation token для управления таймаутами.
            // Для этого требуется выставить главный таймаут в максимальное значение, т.к. он будет перекрывать значение,
            // заданное в cancellation token.
            Timeout = System.Threading.Timeout.InfiniteTimeSpan;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="RestHttpClient" />.
        /// </summary>
        /// <param name="basicHttpService">The basic HTTP service.</param>
        /// <param name="handler">The handler.</param>
        public RestHttpClient(BasicHttpService basicHttpService, HttpMessageHandler handler)
            : base(handler)
        {
            BasicHttpService = basicHttpService;
            _log = BasicHttpService.LoggerFactory.CreateLogger<RestHttpClient>();

            // Для переиспользования экземпляра HttpClient будем использовать cancellation token для управления таймаутами.
            // Для этого требуется выставить главный таймаут в максимальное значение, т.к. он будет перекрывать значение,
            // заданное в cancellation token.
            Timeout = System.Threading.Timeout.InfiniteTimeSpan;
        }

        /// <summary>
        /// Выполнить HTTP DELETE запрос, и вернуть результат десериализованный в тип <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TResult">Тип результата.</typeparam>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="timeout">Таймаут ожидания ответа.</param>
        /// <exception cref="ResponseException"></exception>
        public Task<RestHttpResponseMessage<TResult?>> Delete<TResult>(string uri, TimeSpan timeout = default) =>
            MakeRequestWithoutBody<TResult>("DELETE", uri, timeout);

        /// <summary>
        /// Выполнить HTTP DELETE запрос, результат не возвращать.
        /// </summary>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="timeout">Таймаут ожидания ответа.</param>
        /// <exception cref="ResponseException"></exception>
        public Task Delete(string uri, TimeSpan timeout = default) =>
            MakeRequestWithoutBody("DELETE", uri, timeout);

        /// <summary>
        /// Выполнить HTTP DELETE запрос c телом типа <typeparamref name="TRequest"/>, результат не возвращать.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="timeout">Таймаут ожидания ответа.</param>
        /// <exception cref="ResponseException"></exception>
        public Task Delete<TRequest>(string uri, TRequest value, TimeSpan timeout = default) =>
            MakeRequestWithBody("DELETE", uri, value, timeout);

        /// <summary>
        /// Выполнить HTTP Get запрос и вернуть результат десериализованный в тип <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TResult">Тип результата запроса.</typeparam>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="timeout">Таймаут ожидания ответа.</param>
        /// <exception cref="ResponseException"></exception>
        public Task<RestHttpResponseMessage<TResult?>> Get<TResult>(string uri, TimeSpan timeout = default) =>
            MakeRequestWithoutBody<TResult?>("GET", uri, timeout);

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
            // Если одновременно несколько потоков попытаются вызвать метод получения Access токена, то доступ дадим только одному.
            await SemaphoreSlim.WaitAsync();
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                if (AccessToken is not null && ExpiryTime > DateTime.UtcNow && !invokeHandler)
                    return AccessToken;

                _log.LogInformation("Requesting authentication token.");
                var accessTokenResponse = await handler(this);
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
        /// Выполнить HTTP PATCH запрос c телом типа <typeparamref name="TRequest"/>, результат не возвращать.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="timeout">Таймаут ожидания ответа.</param>
        /// <exception cref="ResponseException"></exception>
        public Task Patch<TRequest>(string uri, TRequest value, TimeSpan timeout = default) =>
            MakeRequestWithBody("PATCH", uri, value, timeout);

        /// <summary>
        /// Выполнить HTTP PATCH запрос c телом типа <typeparamref name="TRequest"/> и вернуть результат десериализованный в тип <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <typeparam name="TResult">Тип результата.</typeparam>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="timeout">Таймаут ожидания ответа.</param>
        /// <exception cref="ResponseException"></exception>
        public Task<RestHttpResponseMessage<TResult?>> Patch<TRequest, TResult>(string uri, TRequest value, TimeSpan timeout = default) =>
            MakeRequestWithBody<TRequest, TResult?>("PATCH", uri, value, timeout);

        /// <summary>
        /// Выполнить HTTP POST запрос c телом типа <typeparamref name="TRequest"/> и вернуть результат десериализованный в тип <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <typeparam name="TResult">Тип результата.</typeparam>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="timeout">Таймаут ожидания ответа.</param>
        /// <exception cref="ResponseException"></exception>
        public Task<RestHttpResponseMessage<TResult?>> Post<TRequest, TResult>(string uri, TRequest value, TimeSpan timeout = default) =>
            MakeRequestWithBody<TRequest, TResult?>("POST", uri, value, timeout);

        /// <summary>
        /// Выполнить HTTP POST запрос c телом типа <typeparamref name="TRequest"/>, результат не возвращать.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="timeout">Таймаут ожидания ответа.</param>
        /// <exception cref="ResponseException"></exception>
        public Task Post<TRequest>(string uri, TRequest value, TimeSpan timeout = default) =>
            MakeRequestWithBody("POST", uri, value, timeout);

        /// <summary>
        /// Выполнить HTTP PUT запрос c телом типа <typeparamref name="TRequest"/> и вернуть результат десериализованный в тип <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <typeparam name="TResult">Тип результата.</typeparam>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="timeout">Таймаут ожидания ответа.</param>
        /// <exception cref="ResponseException"></exception>
        public Task<RestHttpResponseMessage<TResult?>> Put<TRequest, TResult>(string uri, TRequest value, TimeSpan timeout = default) =>
            MakeRequestWithBody<TRequest, TResult?>("PUT", uri, value, timeout);

        /// <summary>
        /// Выполнить HTTP PUT запрос c телом типа <typeparamref name="TRequest"/>, результат не возвращать.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="timeout">Таймаут ожидания ответа.</param>
        /// <exception cref="ResponseException"></exception>
        public Task Put<TRequest>(string uri, TRequest value, TimeSpan timeout = default) =>
            MakeRequestWithBody("PUT", uri, value, timeout);

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

        Dictionary<string, string> GetForwardedHeaders()
        {
            var headers = new Dictionary<string, string>();
            if (BasicHttpService.HttpContextAccessor is not null && BasicHttpService.Configuration.RestHttpClientHeaderOptions.LogForwardedHeaders)
            {
                foreach (var header in BasicHttpService.Configuration.RestHttpClientHeaderOptions.ForwardedHeaders)
                {
                    var requestHeaderValue = (string?)BasicHttpService.HttpContextAccessor?.HttpContext?.Request?.Headers[header];
                    if (!string.IsNullOrEmpty(requestHeaderValue))
                        headers.Add(header, requestHeaderValue);
                }
            }

            return headers;
        }

        Uri GetAbsoluteUri(string uri)
        {
            if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                return new Uri(BaseAddress, uri);
            return new Uri(uri);
        }

        void CheckStatusCode(string method, Uri uri, HttpResponseMessage response, string? requestData, string? responseData, Stopwatch sw)
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

        [return: NotNull]
        async Task<RestHttpResponseMessage<TResult?>> MakeRequestWithBody<TRequest, TResult>(string requestType,
            string uri,
            TRequest value,
            TimeSpan timeout)
        {
            var sw = new Stopwatch();
            sw.Start();
            // Выполняем проброс указанных заголовков в опциях в нижестоящие сервисы.
            PassThroughForwardedHeaders();

            var fullUri = GetAbsoluteUri(uri);
            LogStartEvent(requestType, fullUri);

            var result = string.Empty;
            HttpResponseMessage response;
            using var cts = CreateTimeoutCancelToken(timeout);
            var serializedRequestValue = RestHttpClientSerializer.Serialize(value);
            try
            {
                await SetToken();

                var method = new HttpMethod(requestType);
                var request = new HttpRequestMessage(method, uri)
                {
                    Content = new StringContent(serializedRequestValue, Encoding.UTF8, "application/json")
                };

                response = await SendAsync(request, cts.Token);
                response.RequestMessage = request;

                // Перезапросить токен при ответе 401
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await SetToken(true);
                    // Нельзя послать 2 раза одинаковый запрос.
                    request = new HttpRequestMessage(method, uri);
                    response = await SendAsync(request, cts.Token);
                    response.RequestMessage = request;
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

        void PassThroughForwardedHeaders()
        {
            if (BasicHttpService.HttpContextAccessor is null)
                return;

            foreach (var header in BasicHttpService.Configuration.RestHttpClientHeaderOptions.ForwardedHeaders)
            {
                var requestHeaderValue = (string?)BasicHttpService.HttpContextAccessor.HttpContext?.Request.Headers[header];
                if (string.IsNullOrEmpty(requestHeaderValue)
                    || DefaultRequestHeaders.Contains(header))
                    continue;

                DefaultRequestHeaders.Add(header, requestHeaderValue);
            }
        }

        async Task MakeRequestWithBody<TRequest>(string requestType, string uri, TRequest value, TimeSpan timeout)
        {
            await MakeRequestWithBody<TRequest, object>(requestType, uri, value, timeout);
        }

        [return: NotNull]
        async Task<RestHttpResponseMessage<TResult?>> MakeRequestWithoutBody<TResult>(string requestType, string uri, TimeSpan timeout)
        {
            var sw = new Stopwatch();
            sw.Start();
            // Выполняем проброс указанных заголовков в опциях в нижестоящие сервисы.
            PassThroughForwardedHeaders();

            var fullUri = GetAbsoluteUri(uri);
            LogStartEvent(requestType, fullUri);
            HttpResponseMessage response;
            using var cts = CreateTimeoutCancelToken(timeout);
            var result = string.Empty;
            try
            {
                await SetToken();

                var method = new HttpMethod(requestType);

                var request = new HttpRequestMessage(method, uri);
                response = await SendAsync(request, cts.Token);
                response.RequestMessage = request;

                // Перезапросить токен при ответе 401
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await SetToken(true);
                    // Нельзя послать 2 раза одинаковый запрос.
                    request = new HttpRequestMessage(method, uri);
                    response = await SendAsync(request, cts.Token);
                    response.RequestMessage = request;
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

        CancellationTokenSource CreateTimeoutCancelToken(TimeSpan timeout)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(timeout == default ? _defaultTimeout : timeout);
            return cts;
        }

        async Task MakeRequestWithoutBody(string requestType, string uri, TimeSpan timeout) =>
            await MakeRequestWithoutBody<object>(requestType, uri, timeout);

        async Task SetToken(bool invokeHandler = false)
        {
            var token = await GetAccessToken(invokeHandler);
            if (token != null)
                this.SetBearerToken(token.AccessToken);
        }
    }
}