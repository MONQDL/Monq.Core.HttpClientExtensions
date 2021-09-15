namespace Monq.Core.HttpClientExtensions
{
    /// <summary>
    /// Basic http client configuration.
    /// </summary>
    public class RestHttpClientOptions
    {
        /// <summary>
        /// Configuration of handling http-headers.
        /// </summary>
        public RestHttpClientHeaderOptions RestHttpClientHeaderOptions { get; protected set; } = 
            new RestHttpClientHeaderOptions();

        /// <summary>
        /// Configure processing of request headers.
        /// </summary>
        /// <param name="options">Configuration of handling http-headers.</param>
        public void ConfigHeaders(RestHttpClientHeaderOptions options) =>
            RestHttpClientHeaderOptions = options;
    }
}
