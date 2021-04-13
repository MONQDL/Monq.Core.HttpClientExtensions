using System.Net.Http;

namespace Monq.Core.HttpClientExtensions
{
    /// <summary>
    /// Расширенная версия класса <see cref="HttpResponseMessage"/>,
    /// которая дает готовые методы для получения десериализованных объектов по основным запросам.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public class RestHttpResponseMessage<TResult>
    {
        /// <summary>
        /// Объект, десериализованный из формата JSON в тип <typeparamref name="TResult"/>.
        /// </summary>
        public TResult? ResultObject { get; set; }

        /// <summary>
        /// Ответ от клиента HttpClient.
        /// </summary>
        public HttpResponseMessage OriginalResponse { get; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="RestHttpResponseMessage{TResult}"/>.
        /// </summary>
        /// <param name="responseMessage">Ответ от клиента HttpClient.</param>
        public RestHttpResponseMessage(HttpResponseMessage responseMessage)
        {
            OriginalResponse = responseMessage;
        }
    }
}
