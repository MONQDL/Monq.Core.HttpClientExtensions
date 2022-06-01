using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Monq.Core.HttpClientExtensions.Extensions
{
    /// <summary>
    /// Extension methods for a basic http client (<see cref = "HttpClient" />).
    /// </summary>
    static class HttpClientExtensions
    {
        /// <summary>
        /// Execute an Http POST request to the address <paramref name = "uri" /> and the request body <paramref name = "value" />.
        /// </summary>
        /// <param name="httpClient">The HttpClient.</param>
        /// <param name="uri">The request uri.</param>
        /// <param name="value">The request body.</param>
        public static Task<HttpResponseMessage> PostAsJsonAsync(this HttpClient httpClient, string uri, object value) =>
            MakeRequest(httpClient, "POST", uri, value);

        /// <summary>
        /// Execute an Http PUT request to the address <paramref name = "uri" /> and the request body <paramref name = "value" />.
        /// </summary>
        /// <param name="httpClient">The HttpClient.</param>
        /// <param name="uri">The request uri.</param>
        /// <param name="value">The request body.</param>
        public static Task<HttpResponseMessage> PutAsJsonAsync(this HttpClient httpClient, string uri, object value) =>
            MakeRequest(httpClient, "PUT", uri, value);

        /// <summary>
        /// Execute an Http PATCH request at the address <paramref name = "uri" /> and with the request body <paramref name = "value" />.
        /// </summary>
        /// <param name="httpClient">The HttpClient.</param>
        /// <param name="uri">The request uri.</param>
        /// <param name="value">The request body.</param>
        public static Task<HttpResponseMessage> PatchAsJsonAsync(this HttpClient httpClient, string uri, object value) =>
            MakeRequest(httpClient, "PATCH", uri, value);

        /// <summary>
        /// Execute an Http DELETE request to the address <paramref name = "uri" /> and the request body <paramref name = "value" />.
        /// </summary>
        /// <param name="httpClient">The HttpClient.</param>
        /// <param name="uri">The request uri.</param>
        /// <param name="value">The request body.</param>
        public static Task<HttpResponseMessage> DeleteAsJsonAsync(this HttpClient httpClient, string uri, object value) =>
            MakeRequest(httpClient, "DELETE", uri, value);

        static async Task<HttpResponseMessage> MakeRequest(HttpClient httpClient, 
            string requestType, 
            string uri, 
            object value)
        {
            var cts = new CancellationTokenSource();

            cts.CancelAfter(httpClient.Timeout);

            var method = new HttpMethod(requestType);

            var serializedRequestValue = RestHttpClientSerializer.Serialize(value);
            var request = new HttpRequestMessage(method, uri)
            {
                Content = new StringContent(serializedRequestValue, Encoding.UTF8, "application/json")
            };

            return await httpClient.SendAsync(request, cts.Token);
        }
    }
}
