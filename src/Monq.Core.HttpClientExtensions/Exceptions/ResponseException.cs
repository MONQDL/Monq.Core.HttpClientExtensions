using System;
using System.Net;

namespace Monq.Core.HttpClientExtensions.Exceptions
{
    /// <summary>
    /// The class represents an extended version of the exception for serving <see cref="RestHttpClient"/> requests.
    /// </summary>
    public class ResponseException : Exception
    {
        /// <summary>
        /// HttpClient response code.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// A string with the response data for the Http request.
        /// </summary>
        public string? ResponseData { get; }

        /// <summary>
        /// Initializes a new instance of the class <see cref = "ResponseException" />.
        /// </summary>
        /// <param name="message">A message describing the current exception.</param>
        /// <param name="statusCode">HttpClient response code.</param>
        public ResponseException(string message, HttpStatusCode statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Initializes a new instance of the class <see cref = "ResponseException" />.
        /// </summary>
        /// <param name="message">A message describing the current exception.</param>
        /// <param name="statusCode">HttpClient response code.</param>
        /// <param name="responseData">A string with the response data for the Http request.</param>
        public ResponseException(string message, HttpStatusCode statusCode, string? responseData)
            : this(message, statusCode)
        {
            ResponseData = responseData;
        }
    }
}
