using System;
using System.Net;

namespace Monq.Core.HttpClientExtensions.Exceptions
{
    /// <summary>
    /// Класс представляет расширенную версия исключения для обслуживания запросов RestHttpClient
    /// </summary>
    public class ResponseException : Exception
    {
        /// <summary>
        /// Код ответа HttpClient.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Строка с данными ответа на Http запрос.
        /// </summary>
        public string? ResponseData { get; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ResponseException" />.
        /// </summary>
        /// <param name="message">Сообщение, описывающее текущее исключение.</param>
        /// <param name="statusCode">Код ответа HttpClient.</param>
        public ResponseException(string message, HttpStatusCode statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ResponseException" />.
        /// </summary>
        /// <param name="message">Сообщение, описывающее текущее исключение.</param>
        /// <param name="statusCode">Код ответа HttpClient.</param>
        /// <param name="responseData">Строка с данными ответа на Http запрос.</param>
        public ResponseException(string message, HttpStatusCode statusCode, string? responseData)
            : this(message, statusCode)
        {
            ResponseData = responseData;
        }
    }
}
