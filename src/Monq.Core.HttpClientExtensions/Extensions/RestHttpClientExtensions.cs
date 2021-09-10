using System;
using System.Threading;
using System.Threading.Tasks;

namespace Monq.Core.HttpClientExtensions.Services
{
    /// <summary>
    /// Методы расширения для базового http-клиента (<see cref="RestHttpClient"/>).
    /// </summary>
    static class RestHttpClientExtensions
    {
        public static CancellationTokenSource CreateTimeoutCancelToken(this RestHttpClient restHttpClient, TimeSpan timeout) =>
            new CancellationTokenSource(timeout == default ? restHttpClient.DefaultTimeout : timeout);

        /// <summary>
        /// Выполнить HTTP Get запрос и вернуть результат десериализованный в тип <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TResult">Тип результата запроса.</typeparam>
        /// <param name="restHttpClient">The RestHttpClient instance.</param>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="timeout">Таймаут ожидания ответа.</param>
        /// <exception cref="Exceptions.ResponseException"></exception>
        public static Task<RestHttpResponseMessage<TResult>> Get<TResult>(this RestHttpClient restHttpClient,
            string uri,
            TimeSpan timeout) =>
                restHttpClient.Get<TResult>(uri, restHttpClient.CreateTimeoutCancelToken(timeout).Token);

        /// <summary>
        /// Выполнить HTTP POST запрос c телом типа <typeparamref name="TRequest"/> и вернуть результат десериализованный в тип <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <typeparam name="TResult">Тип результата.</typeparam>
        /// <param name="restHttpClient">The RestHttpClient instance.</param>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="timeout">Таймаут ожидания ответа.</param>
        /// <exception cref="Exceptions.ResponseException"></exception>
        public static Task<RestHttpResponseMessage<TResult?>> Post<TRequest, TResult>(this RestHttpClient restHttpClient,
            string uri,
            TRequest value,
            TimeSpan timeout) =>
                restHttpClient.Post<TRequest, TResult?>(uri, value, restHttpClient.CreateTimeoutCancelToken(timeout).Token);

        /// <summary>
        /// Выполнить HTTP POST запрос c телом типа <typeparamref name="TRequest"/>, результат не возвращать.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <param name="restHttpClient">The RestHttpClient instance.</param>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="timeout">Таймаут ожидания ответа.</param>
        /// <exception cref="Exceptions.ResponseException"></exception>
        public static Task Post<TRequest>(this RestHttpClient restHttpClient,
            string uri,
            TRequest value,
            TimeSpan timeout) =>
                restHttpClient.Post(uri, value, restHttpClient.CreateTimeoutCancelToken(timeout).Token);

        /// <summary>
        /// Выполнить HTTP PUT запрос c телом типа <typeparamref name="TRequest"/> и вернуть результат десериализованный в тип <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <typeparam name="TResult">Тип результата.</typeparam>
        /// <param name="restHttpClient">The RestHttpClient instance.</param>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="timeout">Таймаут ожидания ответа.</param>
        /// <exception cref="Exceptions.ResponseException"></exception>
        public static Task<RestHttpResponseMessage<TResult?>> Put<TRequest, TResult>(this RestHttpClient restHttpClient,
            string uri,
            TRequest value,
            TimeSpan timeout) =>
                restHttpClient.Put<TRequest, TResult?>(uri, value, restHttpClient.CreateTimeoutCancelToken(timeout).Token);

        /// <summary>
        /// Выполнить HTTP PUT запрос c телом типа <typeparamref name="TRequest"/>, результат не возвращать.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <param name="restHttpClient">The RestHttpClient instance.</param>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="timeout">Таймаут ожидания ответа.</param>
        /// <exception cref="Exceptions.ResponseException"></exception>
        public static Task Put<TRequest>(this RestHttpClient restHttpClient,
            string uri,
            TRequest value,
            TimeSpan timeout) =>
                restHttpClient.Put(uri, value, restHttpClient.CreateTimeoutCancelToken(timeout).Token);

        /// <summary>
        /// Выполнить HTTP PATCH запрос c телом типа <typeparamref name="TRequest"/>, результат не возвращать.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <param name="restHttpClient">The RestHttpClient instance.</param>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="timeout">Таймаут ожидания ответа.</param>
        /// <exception cref="Exceptions.ResponseException"></exception>
        public static Task Patch<TRequest>(this RestHttpClient restHttpClient,
            string uri,
            TRequest value,
            TimeSpan timeout) =>
                restHttpClient.Patch(uri, value, restHttpClient.CreateTimeoutCancelToken(timeout).Token);

        /// <summary>
        /// Выполнить HTTP PATCH запрос c телом типа <typeparamref name="TRequest"/> и вернуть результат десериализованный в тип <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <typeparam name="TResult">Тип результата.</typeparam>
        /// <param name="restHttpClient">The RestHttpClient instance.</param>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="timeout">Таймаут ожидания ответа.</param>
        /// <exception cref="Exceptions.ResponseException"></exception>
        public static Task<RestHttpResponseMessage<TResult?>> Patch<TRequest, TResult>(this RestHttpClient restHttpClient,
            string uri,
            TRequest value,
            TimeSpan timeout) =>
                restHttpClient.Patch<TRequest, TResult?>(uri, value, restHttpClient.CreateTimeoutCancelToken(timeout).Token);

        /// <summary>
        /// Выполнить HTTP DELETE запрос, результат не возвращать.
        /// </summary>
        /// <param name="restHttpClient">The RestHttpClient instance.</param>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="timeout">Таймаут ожидания ответа.</param>
        public static Task Delete(this RestHttpClient restHttpClient, string uri, TimeSpan timeout) =>
            restHttpClient.Delete(uri, restHttpClient.CreateTimeoutCancelToken(timeout).Token);

        /// <summary>
        /// Выполнить HTTP DELETE запрос, и вернуть результат десериализованный в тип <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TResult">Тип результата.</typeparam>
        /// <param name="restHttpClient">The RestHttpClient instance.</param>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="timeout">Таймаут ожидания ответа.</param>
        /// <exception cref="Exceptions.ResponseException"></exception>
        public static Task<RestHttpResponseMessage<TResult?>> Delete<TResult>(this RestHttpClient restHttpClient,
            string uri,
            TimeSpan timeout) =>
            restHttpClient.Delete<TResult>(uri, restHttpClient.CreateTimeoutCancelToken(timeout).Token);

        /// <summary>
        /// Выполнить HTTP DELETE запрос c телом типа <typeparamref name="TRequest"/>, результат не возвращать.
        /// </summary>
        /// <typeparam name="TRequest">Тип тела запроса.</typeparam>
        /// <param name="restHttpClient">The RestHttpClient instance.</param>
        /// <param name="uri">Абсолютный Uri вызываемого сервиса.</param>
        /// <param name="value">Объект, который требуется сериализовать как тело запроса.</param>
        /// <param name="timeout">Таймаут ожидания ответа.</param>
        /// <exception cref="Exceptions.ResponseException"></exception>
        public static Task Delete<TRequest>(this RestHttpClient restHttpClient, 
            string uri, 
            TRequest value, 
            TimeSpan timeout) =>
                restHttpClient.Delete(uri, value, restHttpClient.CreateTimeoutCancelToken(timeout).Token);
    }
}
