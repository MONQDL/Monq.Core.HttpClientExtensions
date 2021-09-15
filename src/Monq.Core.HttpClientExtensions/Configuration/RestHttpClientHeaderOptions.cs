using System.Collections.Generic;

namespace Monq.Core.HttpClientExtensions
{
    /// <summary>
    /// Configuration of handling http-headers.
    /// </summary>
    public class RestHttpClientHeaderOptions
    {
        /// <summary>
        /// The list of header names that will be forwarded to the downstream service 
        /// when calling the HttpClientExtensions methods.
        /// </summary>
        public HashSet<string> ForwardedHeaders { get; set; } = new HashSet<string>();

        /// <summary>
        /// The flag sets the behavior when header values passed through <see cref = "ForwardedHeaders" /> will be displayed in the logs.
        /// The default is true.
        /// </summary>
        public bool LogForwardedHeaders { get; set; } = true;

        /// <summary>
        /// Add the header to the list of headers that will be forwarded to the downstream service when calling the HttpClientExtensions methods.
        /// </summary>
        /// <param name="header">Header name.</param>
        public RestHttpClientHeaderOptions AddForwardedHeader(string header)
        {
            if (!ForwardedHeaders.Contains(header))
                ForwardedHeaders.Add(header);

            return this;
        }
    }
}