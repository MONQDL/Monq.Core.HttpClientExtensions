using System.Net.Http;

namespace Monq.Core.HttpClientExtensions
{
    /// <summary>
    /// An extended version of the <see cref = "HttpResponseMessage" /> class, 
    /// which provides ready-made methods for getting deserialized objects for basic requests.
    /// </summary>
    /// <typeparam name="TResult">The Response result type.</typeparam>
    public class RestHttpResponseMessage<TResult>
    {
        /// <summary>
        /// An object deserialized from JSON to type <typeparamref name = "TResult" />.
        /// </summary>
        public TResult? ResultObject { get; set; }

        /// <summary>
        /// Original response from the HttpClient.
        /// </summary>
        public HttpResponseMessage OriginalResponse { get; }

        /// <summary>
        /// Initializes a new instance of the class <see cref = "RestHttpResponseMessage {TResult}" />.
        /// </summary>
        /// <param name="responseMessage">The HttpClient Response.</param>
        public RestHttpResponseMessage(HttpResponseMessage responseMessage)
        {
            OriginalResponse = responseMessage;
        }
    }
}
