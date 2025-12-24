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
    /// The HTTP data handler stack used to send requests with advanced logging and a set of simplified request rules.
    /// </summary>
    /// <seealso cref="HttpClient" />
    public partial class RestHttpClient
    {
        static readonly object _lockObj = new object();

        /// <summary>
        /// Semaphore for synchronizing AccessToken receiving streams.
        /// </summary>
        static readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

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
        /// Access Token to be used for Http requests.
        /// </summary>
        public static TokenResponse? AccessToken { get; private set; }

        /// <summary>
        /// An event handler for getting the AccessToken.
        /// </summary>
        public delegate Task<TokenResponse> AuthorizationRequestHandler(HttpClient client);

        /// <summary>
        /// When this event is called, it is required to obtain an AccessToken from the Identity of the server.
        /// </summary>
        public static event AuthorizationRequestHandler? AuthorizationRequest;

        const string BearerIdentifier = "Bearer";
        const string AuthorizationHeader = "Authorization";

        /// <summary>
        /// The logger factory.
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
        /// Execute an HTTP DELETE request, and return the result deserialized to type <typeparamref name = "TResult" />.
        /// </summary>
        /// <typeparam name="TResult">The Result type.</typeparam>
        /// <param name="uri">The Uri of the service being called.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="headers">The Http request headers, that will be set to the HttpRequestMessage.</param>
        /// <param name="serializer">Custom serializer for the current request only.</param>
        /// <exception cref="ResponseException"></exception>
        [RequiresUnreferencedCode(
            "Serializers is incompatible with trimming.")]
        public Task<RestHttpResponseMessage<TResult?>> Delete<TResult>(string uri,
            CancellationToken cancellationToken = default,
            IHeaderDictionary? headers = default,
            IRestHttpClientSerializer? serializer = default) =>
            MakeRequestWithoutBody<TResult?>("DELETE", uri, cancellationToken, headers, serializer);

        /// <summary>
        /// Execute an HTTP DELETE request, return no result.
        /// </summary>
        /// <param name="uri">The Uri of the service being called.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="headers">The Http request headers, that will be set to the HttpRequestMessage.</param>
        /// <param name="serializer">Custom serializer for the current request only.</param>
        /// <exception cref="ResponseException"></exception>
        [RequiresUnreferencedCode(
            "Serializers is incompatible with trimming.")]
        public Task Delete(string uri,
            CancellationToken cancellationToken = default,
            IHeaderDictionary? headers = default,
            IRestHttpClientSerializer? serializer = default) =>
           MakeRequestWithoutBody("DELETE", uri, cancellationToken, headers, serializer);

        /// <summary>
        /// Execute an HTTP DELETE request with a body of type <typeparamref name = "TRequest" />, do not return the result.
        /// </summary>
        /// <typeparam name="TRequest">The Request body type.</typeparam>
        /// <param name="uri">The Uri of the service being called.</param>
        /// <param name="value">The object to serialize as the request body.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="headers">The Http request headers, that will be set to the HttpRequestMessage.</param>
        /// <param name="serializer">Custom serializer for the current request only.</param>
        /// <exception cref="ResponseException"></exception>
        [RequiresUnreferencedCode(
            "Serializers is incompatible with trimming.")]
        public Task Delete<TRequest>(string uri,
            TRequest value,
            CancellationToken cancellationToken = default,
            IHeaderDictionary? headers = default,
            IRestHttpClientSerializer? serializer = default) =>
            MakeRequestWithBody<TRequest, object>("DELETE", uri, value, cancellationToken, headers, serializer);

        /// <summary>
        /// Execute an HTTP Get request and return the result deserialized to type <typeparamref name = "TResult" />.
        /// </summary>
        /// <typeparam name="TResult">The query result type.</typeparam>
        /// <param name="uri">The Uri of the service being called.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="headers">The Http request headers, that will be set to the HttpRequestMessage.</param>
        /// <param name="serializer">Custom serializer for the current request only.</param>
        /// <exception cref="ResponseException"></exception>
        [RequiresUnreferencedCode(
            "Serializers is incompatible with trimming.")]
        public Task<RestHttpResponseMessage<TResult?>> Get<TResult>(string uri,
            CancellationToken cancellationToken = default,
            IHeaderDictionary? headers = default,
            IRestHttpClientSerializer? serializer = default) =>
            MakeRequestWithoutBody<TResult>("GET", uri, cancellationToken, headers, serializer);

        /// <summary>
        /// Execute an HTTP PATCH request with a body of type <typeparamref name = "TRequest" />, do not return the result.
        /// </summary>
        /// <typeparam name="TRequest">The Request body type.</typeparam>
        /// <param name="uri">The Uri of the service being called.</param>
        /// <param name="value">The object to serialize as the request body.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="headers">The Http request headers, that will be set to the HttpRequestMessage.</param>
        /// <param name="serializer">Custom serializer for the current request only.</param>
        /// <exception cref="ResponseException"></exception>
        [RequiresUnreferencedCode(
            "Serializers is incompatible with trimming.")]
        public Task Patch<TRequest>(string uri,
            TRequest value,
            CancellationToken cancellationToken = default,
            IHeaderDictionary? headers = default,
            IRestHttpClientSerializer? serializer = default) =>
            MakeRequestWithBody("PATCH", uri, value, cancellationToken, headers, serializer);

        /// <summary>
        /// Execute an HTTP PATCH request with a body of type <typeparamref name = "TRequest" /> 
        /// and return the result deserialized to type <typeparamref name = "TResult" />.
        /// </summary>
        /// <typeparam name="TRequest">The Request body type.</typeparam>
        /// <typeparam name="TResult">The Result type.</typeparam>
        /// <param name="uri">The Uri of the service being called.</param>
        /// <param name="value">The object to serialize as the request body.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="headers">The Http request headers, that will be set to the HttpRequestMessage.</param>
        /// <param name="serializer">Custom serializer for the current request only.</param>
        /// <exception cref="ResponseException"></exception>
        [RequiresUnreferencedCode(
            "Serializers is incompatible with trimming.")]
        public Task<RestHttpResponseMessage<TResult?>> Patch<TRequest, TResult>(string uri,
            TRequest value,
            CancellationToken cancellationToken = default,
            IHeaderDictionary? headers = default,
            IRestHttpClientSerializer? serializer = default) =>
            MakeRequestWithBody<TRequest, TResult?>("PATCH", uri, value, cancellationToken, headers, serializer);

        /// <summary>
        /// Execute an HTTP POST request with a body of type <typeparamref name = "TRequest" /> 
        /// and return the result deserialized to type <typeparamref name = "TResult" />.
        /// </summary>
        /// <typeparam name="TRequest">The Request body type.</typeparam>
        /// <typeparam name="TResult">The Result type.</typeparam>
        /// <param name="uri">The Uri of the service being called.</param>
        /// <param name="value">The object to serialize as the request body.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="headers">The Http request headers, that will be set to the HttpRequestMessage.</param>
        /// <param name="serializer">Custom serializer for the current request only.</param>
        /// <exception cref="ResponseException"></exception>
        [RequiresUnreferencedCode(
            "Serializers is incompatible with trimming.")]
        public Task<RestHttpResponseMessage<TResult?>> Post<TRequest, TResult>(string uri,
            TRequest value,
            CancellationToken cancellationToken = default,
            IHeaderDictionary? headers = default,
            IRestHttpClientSerializer? serializer = default) =>
            MakeRequestWithBody<TRequest, TResult?>("POST", uri, value, cancellationToken, headers, serializer);

        /// <summary>
        /// Execute an HTTP POST request with a body of type <typeparamref name = "TRequest" />, do not return the result.
        /// </summary>
        /// <typeparam name="TRequest">The Request body type.</typeparam>
        /// <param name="uri">The Uri of the service being called.</param>
        /// <param name="value">The object to serialize as the request body.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="headers">The Http request headers, that will be set to the HttpRequestMessage.</param>
        /// <param name="serializer">Custom serializer for the current request only.</param>
        /// <exception cref="ResponseException"></exception>
        [RequiresUnreferencedCode(
            "Serializers is incompatible with trimming.")]
        public Task Post<TRequest>(string uri,
            TRequest value,
            CancellationToken cancellationToken = default,
            IHeaderDictionary? headers = default,
            IRestHttpClientSerializer? serializer = default) =>
            MakeRequestWithBody("POST", uri, value, cancellationToken, headers, serializer);

        /// <summary>
        /// Execute an HTTP PUT request with a body of type <typeparamref name = "TRequest" /> 
        /// and return the result deserialized to type <typeparamref name = "TResult" />.
        /// </summary>
        /// <typeparam name="TRequest">The Request body type.</typeparam>
        /// <typeparam name="TResult">The Result type.</typeparam>
        /// <param name="uri">The Uri of the service being called.</param>
        /// <param name="value">The object to serialize as the request body.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="headers">The Http request headers, that will be set to the HttpRequestMessage.</param>
        /// <param name="serializer">Custom serializer for the current request only.</param>
        /// <exception cref="ResponseException"></exception>
        [RequiresUnreferencedCode(
            "Serializers is incompatible with trimming.")]
        public Task<RestHttpResponseMessage<TResult?>> Put<TRequest, TResult>(string uri,
            TRequest value,
            CancellationToken cancellationToken = default,
            IHeaderDictionary? headers = default,
            IRestHttpClientSerializer? serializer = default) =>
            MakeRequestWithBody<TRequest, TResult?>("PUT", uri, value, cancellationToken, headers, serializer);

        /// <summary>
        /// Execute an HTTP PUT request with a body of type <typeparamref name = "TRequest" />, do not return the result.
        /// </summary>
        /// <typeparam name="TRequest">The Request body type.</typeparam>
        /// <param name="uri">The Uri of the service being called.</param>
        /// <param name="value">The object to serialize as the request body.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="headers">The Http request headers, that will be set to the HttpRequestMessage.</param>
        /// <param name="serializer">Custom serializer for the current request only.</param>
        /// <exception cref="ResponseException"></exception>
        [RequiresUnreferencedCode(
            "Serializers is incompatible with trimming.")]
        public Task Put<TRequest>(string uri,
            TRequest value,
            CancellationToken cancellationToken = default,
            IHeaderDictionary? headers = default,
            IRestHttpClientSerializer? serializer = default) =>
            MakeRequestWithBody("PUT", uri, value, cancellationToken, headers, serializer);

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
            // If several threads try to call the method for obtaining the Access token at the same time,
            // we will give access to only one.
            await _semaphoreSlim.WaitAsync();
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                if (AccessToken is not null && ExpiryTime > DateTime.UtcNow && !invokeHandler)
                    return AccessToken;

                _log.LogInformation("Requesting authentication token.");
                var accessTokenResponse = await handler(_httpClient);
                sw.Stop();
                _log.LogInformation("Authentication token request finished at {ElapsedMilliseconds} ms.",
                    sw.ElapsedMilliseconds);
                if (accessTokenResponse.IsError)
                {
                    throw new SecurityTokenException("Could not retrieve token.");
                }

                // Set Token to the new token and set the expiry time to the new expiry time.
                AccessToken = accessTokenResponse;
                ExpiryTime = DateTime.UtcNow.AddSeconds(AccessToken.ExpiresIn);

                // Return fresh token.
                return AccessToken;
            }
            catch (Exception e)
            {
                _log.LogCritical(e, "Raised error during authentication token request. Details: {ErrorMessage}", e.Message);
            }
            finally
            {
                sw.Stop();
                _semaphoreSlim.Release();
            }

            return null;
        }

        /// <summary>
        /// Resets the access token.
        /// </summary>
        public static void ResetAccessToken()
        {
            lock (_lockObj)
            {
                AccessToken = null;
            }
        }

        /// <summary>
        /// Remove all subscribers to the AuthorizationRequest event.
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
                "Downstream request {Method} {Path} with http forwarded headers={HttpForwardedHeaders} failed with " +
                "StatusCode {StatusCode} at {ElapsedMilliseconds} ms. Request body: {ServiceRequestData}. " +
                "Response body: {ServiceResponseData}.",
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
                     "Downstream request {Method} {Path} with http forwarded headers={HttpForwardedHeaders} finished with " +
                     "StatusCode {StatusCode} at {ElapsedMilliseconds} ms.",
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
                "Downstream request {Method} {Path} with http forwarded headers={HttpForwardedHeaders} failed with " +
                "Exception at {ElapsedMilliseconds} ms. " +
                "Request body: {ServiceRequestData}. Response body: {ServiceResponseData}. Exception message: {ErrorMessage}",
                method,
                uri.ToString(),
                headers,
                sw.ElapsedMilliseconds,
                requestData,
                responseData,
                e.Message);
        }

        void LogStartEvent(string method, Uri uri)
        {
            var headers = GetForwardedHeaders();
            _log.LogInformation(
                     new EventId(TraceConstants.DownServiceEventId),
                     "Start downstream request {Method} {Path} with http forwarded headers={HttpForwardedHeaders}.",
                     method,
                     uri.ToString(),
                     headers);
        }

        [RequiresUnreferencedCode(
            "Serializers is incompatible with trimming.")]
        Task MakeRequestWithBody<TRequest>(string requestType,
            string uri,
            TRequest value,
            CancellationToken? cancellationToken = default,
            IHeaderDictionary? headers = default,
            IRestHttpClientSerializer? serializer = default) =>
            MakeRequestWithBody<TRequest, object>(requestType, uri, value, cancellationToken, headers, serializer);

        /// <summary>
        /// Make request without body
        /// </summary>
        [RequiresUnreferencedCode(
            "Serializers is incompatible with trimming.")]
        [return: NotNull]
        protected virtual async Task<RestHttpResponseMessage<TResult?>> MakeRequestWithBody<TRequest, TResult>(
            string requestType,
            string uri,
            TRequest value,
            CancellationToken? cancellationToken = default,
            IHeaderDictionary? headers = default,
            IRestHttpClientSerializer? serializer = default)
        {
            var cts = cancellationToken ?? new CancellationTokenSource(DefaultTimeout).Token;

            var sw = new Stopwatch();
            sw.Start();

            var fullUri = GetAbsoluteUri(uri);
            LogStartEvent(requestType, fullUri);

            var result = string.Empty;
            HttpResponseMessage response;
            var jsonSerializer = serializer ?? RestHttpClientSerializer.CurrentSerializer;
            var serializedRequestValue = jsonSerializer.Serialize(value);
            try
            {
                await SetToken();

                var method = new HttpMethod(requestType);
                response = await DoHttpRequest(uri,
                                     headers,
                                     method,
                                     cts,
                                     new StringContent(serializedRequestValue, Encoding.UTF8, "application/json"));

                // Refresh token on 401 response.
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await SetToken(true);
                    // You cannot send the same request 2 times.
                    response = await DoHttpRequest(uri,
                                         headers,
                                         method,
                                         cts,
                                         new StringContent(serializedRequestValue, Encoding.UTF8, "application/json"));
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

            return new RestHttpResponseMessage<TResult?>(response)
            {
                ResultObject =
                result.JsonToObject<TResult>(serializer)
            };
        }

        [RequiresUnreferencedCode(
            "Serializers is incompatible with trimming.")]
        async Task MakeRequestWithoutBody(string requestType,
            string uri,
            CancellationToken? cancellationToken = default,
            IHeaderDictionary? headers = default,
            IRestHttpClientSerializer? serializer = default) =>
            await MakeRequestWithoutBody<object>(requestType, uri, cancellationToken, headers, serializer);

        /// <summary>
        /// Make request without body
        /// </summary>
        [RequiresUnreferencedCode(
            "Serializers is incompatible with trimming.")]
        [return: NotNull]
        protected virtual async Task<RestHttpResponseMessage<TResult?>> MakeRequestWithoutBody<TResult>(
            string requestType,
            string uri,
            CancellationToken? cancellationToken = default,
            IHeaderDictionary? headers = default,
            IRestHttpClientSerializer? serializer = default)
        {
            var cts = cancellationToken ?? new CancellationTokenSource(DefaultTimeout).Token;

            var sw = new Stopwatch();
            sw.Start();

            var fullUri = GetAbsoluteUri(uri);
            LogStartEvent(requestType, fullUri);
            HttpResponseMessage response;

            string result = string.Empty;
            try
            {
                await SetToken();

                var method = new HttpMethod(requestType);
                response = await DoHttpRequest(uri, headers, method, cts);

                // Refresh token on 401 response.
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await SetToken(true);
                    // You cannot send the same request 2 times.
                    response = await DoHttpRequest(uri, headers, method, cts);
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

            return new RestHttpResponseMessage<TResult?>(response)
            {
                ResultObject = result.JsonToObject<TResult>(serializer)
            };
        }

        async Task<HttpResponseMessage> DoHttpRequest(string uri,
            IHeaderDictionary? headers,
            HttpMethod method,
            CancellationToken cts,
            HttpContent? content = default)
        {
            var request = new HttpRequestMessage(method, uri);
            if (content is not null)
                request.Content = content;
            SetHttpRequestHeaders(request, headers);

            var response = await _httpClient.SendAsync(request, cts);
            response.RequestMessage = request;
            return response;
        }

        void SetHttpRequestHeaders(HttpRequestMessage request, IHeaderDictionary? headers)
        {
            if (headers is not null && headers.Any())
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value.ToArray());
                }
            // Forward the specified headers in the options to the downstream services.
            PassThroughForwardedHeaders(request);
        }

        void PassThroughForwardedHeaders(HttpRequestMessage requestMessage)
        {
            if (HttpContextAccessor is null)
                return;

            foreach (var header in Configuration.RestHttpClientHeaderOptions.ForwardedHeaders)
            {
                var requestHeaderValue = (string?)HttpContextAccessor.HttpContext?.Request.Headers[header];
                if (string.IsNullOrEmpty(requestHeaderValue)
                    || requestMessage.Headers.Contains(header))
                    continue;

                requestMessage.Headers.Add(header, requestHeaderValue);
            }
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
