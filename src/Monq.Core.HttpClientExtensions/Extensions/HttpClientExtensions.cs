using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Monq.Core.HttpClientExtensions.Extensions
{
    /// <summary>
    /// Методы расширения для базового http-клиента (<see cref="RestHttpClient"/>).
    /// </summary>
    static class HttpClientExtensions
    {
        static readonly JsonSerializerOptions SerializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Выполнить Http POST запрос по адресу <paramref name="uri"/> и телом запроса <paramref name="value"/>.
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="uri">Адрес запроса.</param>
        /// <param name="value">Тело запроса.</param>
        public static Task<HttpResponseMessage> PostAsJsonAsync(this HttpClient httpClient, string uri, object value) =>
            MakeRequest(httpClient, "POST", uri, value);

        /// <summary>
        /// Выполнить Http PUT запрос по адресу <paramref name="uri"/> и телом запроса <paramref name="value"/>.
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="uri">Адрес запроса.</param>
        /// <param name="value">Тело запроса.</param>
        public static Task<HttpResponseMessage> PutAsJsonAsync(this HttpClient httpClient, string uri, object value) =>
            MakeRequest(httpClient, "PUT", uri, value);

        /// <summary>
        /// Выполнить Http PATCH запрос по адресу <paramref name="uri"/> и телом запроса <paramref name="value"/>.
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="uri">Адрес запроса.</param>
        /// <param name="value">Тело запроса.</param>
        public static Task<HttpResponseMessage> PatchAsJsonAsync(this HttpClient httpClient, string uri, object value) =>
            MakeRequest(httpClient, "PATCH", uri, value);

        /// <summary>
        /// Выполнить Http DELETE запрос по адресу <paramref name="uri"/> и телом запроса <paramref name="value"/>.
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="uri">Адрес запроса.</param>
        /// <param name="value">Тело запроса.</param>
        public static Task<HttpResponseMessage> DeleteAsJsonAsync(this HttpClient httpClient, string uri, object value) =>
            MakeRequest(httpClient, "DELETE", uri, value);

        static async Task<HttpResponseMessage> MakeRequest(HttpClient httpClient, string requestType, string uri, object value)
        {
            var cts = new CancellationTokenSource();

            cts.CancelAfter(httpClient.Timeout);

            var method = new HttpMethod(requestType);

            var serializedRequestValue = JsonSerializer.Serialize(value, SerializeOptions);
            var request = new HttpRequestMessage(method, uri)
            {
                Content = new StringContent(serializedRequestValue, Encoding.UTF8, "application/json")
            };

            return await httpClient.SendAsync(request, cts.Token);
        }
    }
}
