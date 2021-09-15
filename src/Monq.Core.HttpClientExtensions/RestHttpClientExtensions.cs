using System;
using System.Threading;
using System.Threading.Tasks;

namespace Monq.Core.HttpClientExtensions
{
    /// <summary>
    /// Extension methods for a basic http client (<see cref="RestHttpClient" />).
    /// </summary>
    public partial class RestHttpClient
    {
        public CancellationTokenSource CreateTimeoutCancelToken(TimeSpan timeout) =>
            new CancellationTokenSource(timeout == default ? DefaultTimeout : timeout);

        /// <summary>
        /// Execute an HTTP Get request and return the result deserialized to type <typeparamref name = "TResult" />.
        /// </summary>
        /// <typeparam name="TResult">The query result type.</typeparam>
        /// <param name="uri">The Uri of the service being called.</param>
        /// <param name="timeout">The timeout waiting for a response.</param>
        /// <exception cref="Exceptions.ResponseException"></exception>
        public Task<RestHttpResponseMessage<TResult?>> Get<TResult>(
            string uri,
            TimeSpan timeout) =>
                Get<TResult>(uri, CreateTimeoutCancelToken(timeout).Token);

        /// <summary>
        /// Execute an HTTP POST request with a body of type <typeparamref name = "TRequest" /> 
        /// and return the result deserialized to type <typeparamref name = "TResult" />.
        /// </summary>
        /// <typeparam name="TRequest">The Request body type.</typeparam>
        /// <typeparam name="TResult">The Result type.</typeparam>
        /// <param name="uri">The Uri of the service being called.</param>
        /// <param name="value">The object to serialize as the request body.</param>
        /// <param name="timeout">The timeout waiting for a response.</param>
        /// <exception cref="Exceptions.ResponseException"></exception>
        public Task<RestHttpResponseMessage<TResult?>> Post<TRequest, TResult>(
            string uri,
            TRequest value,
            TimeSpan timeout) =>
                Post<TRequest, TResult?>(uri, value, CreateTimeoutCancelToken(timeout).Token);

        /// <summary>
        /// Execute an HTTP POST request with a body of type <typeparamref name = "TRequest" />, do not return the result.
        /// </summary>
        /// <typeparam name="TRequest">The Request body type.</typeparam>
        /// <param name="uri">The Uri of the service being called.</param>
        /// <param name="value">The object to serialize as the request body.</param>
        /// <param name="timeout">The timeout waiting for a response.</param>
        /// <exception cref="Exceptions.ResponseException"></exception>
        public Task Post<TRequest>(
            string uri,
            TRequest value,
            TimeSpan timeout) =>
                Post(uri, value, CreateTimeoutCancelToken(timeout).Token);

        /// <summary>
        /// Execute an HTTP PUT request with a body of type <typeparamref name = "TRequest" /> 
        /// and return the result deserialized to type <typeparamref name = "TResult" />.
        /// </summary>
        /// <typeparam name="TRequest">The Request body type.</typeparam>
        /// <typeparam name="TResult">The Result type.</typeparam>
        /// <param name="uri">The Uri of the service being called.</param>
        /// <param name="value">The object to serialize as the request body.</param>
        /// <param name="timeout">The timeout waiting for a response.</param>
        /// <exception cref="Exceptions.ResponseException"></exception>
        public Task<RestHttpResponseMessage<TResult?>> Put<TRequest, TResult>(
            string uri,
            TRequest value,
            TimeSpan timeout) =>
                Put<TRequest, TResult?>(uri, value, CreateTimeoutCancelToken(timeout).Token);

        /// <summary>
        /// Execute an HTTP PUT request with a body of type <typeparamref name = "TRequest" />, do not return the result.
        /// </summary>
        /// <typeparam name="TRequest">The Request body type.</typeparam>
        /// <param name="uri">The Uri of the service being called.</param>
        /// <param name="value">The object to serialize as the request body.</param>
        /// <param name="timeout">The timeout waiting for a response.</param>
        /// <exception cref="Exceptions.ResponseException"></exception>
        public Task Put<TRequest>(
            string uri,
            TRequest value,
            TimeSpan timeout) =>
                Put(uri, value, CreateTimeoutCancelToken(timeout).Token);

        /// <summary>
        /// Execute an HTTP PATCH request with a body of type <typeparamref name = "TRequest" />, do not return the result.
        /// </summary>
        /// <typeparam name="TRequest">The Request body type.</typeparam>
        /// <param name="uri">The Uri of the service being called.</param>
        /// <param name="value">The object to serialize as the request body.</param>
        /// <param name="timeout">The timeout waiting for a response.</param>
        /// <exception cref="Exceptions.ResponseException"></exception>
        public Task Patch<TRequest>(
            string uri,
            TRequest value,
            TimeSpan timeout) =>
                Patch(uri, value, CreateTimeoutCancelToken(timeout).Token);

        /// <summary>
        /// Execute an HTTP PATCH request with a body of type <typeparamref name = "TRequest" /> 
        /// and return the result deserialized to type <typeparamref name = "TResult" />.
        /// </summary>
        /// <typeparam name="TRequest">The Request body type.</typeparam>
        /// <typeparam name="TResult">The Result type.</typeparam>
        /// <param name="uri">The Uri of the service being called.</param>
        /// <param name="value">The object to serialize as the request body.</param>
        /// <param name="timeout">The timeout waiting for a response.</param>
        /// <exception cref="Exceptions.ResponseException"></exception>
        public Task<RestHttpResponseMessage<TResult?>> Patch<TRequest, TResult>(
            string uri,
            TRequest value,
            TimeSpan timeout) =>
                Patch<TRequest, TResult?>(uri, value, CreateTimeoutCancelToken(timeout).Token);

        /// <summary>
        /// Execute an HTTP DELETE request, return no result.
        /// </summary>
        /// <param name="uri">The Uri of the service being called.</param>
        /// <param name="timeout">The timeout waiting for a response.</param>
        public Task Delete(string uri, TimeSpan timeout) =>
            Delete(uri, CreateTimeoutCancelToken(timeout).Token);

        /// <summary>
        /// Execute an HTTP DELETE request, and return the result deserialized to type <typeparamref name = "TResult" />.
        /// </summary>
        /// <typeparam name="TResult">The Result type.</typeparam>
        /// <param name="uri">The Uri of the service being called.</param>
        /// <param name="timeout">The timeout waiting for a response.</param>
        /// <exception cref="Exceptions.ResponseException"></exception>
        public Task<RestHttpResponseMessage<TResult?>> Delete<TResult>(
            string uri,
            TimeSpan timeout) =>
            Delete<TResult>(uri, CreateTimeoutCancelToken(timeout).Token);

        /// <summary>
        /// Execute an HTTP DELETE request with a body of type <typeparamref name = "TRequest" />, do not return the result.
        /// </summary>
        /// <typeparam name="TRequest">The Request body type.</typeparam>
        /// <param name="uri">The Uri of the service being called.</param>
        /// <param name="value">The object to serialize as the request body.</param>
        /// <param name="timeout">The timeout waiting for a response.</param>
        /// <exception cref="Exceptions.ResponseException"></exception>
        public Task Delete<TRequest>(
            string uri, 
            TRequest value, 
            TimeSpan timeout) =>
                Delete(uri, value, CreateTimeoutCancelToken(timeout).Token);
    }
}
