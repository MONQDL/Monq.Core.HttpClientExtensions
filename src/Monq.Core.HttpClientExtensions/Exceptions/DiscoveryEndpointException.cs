using System;

namespace Monq.Core.HttpClientExtensions.Exceptions
{
    /// <summary>
    /// Exception throws when discovery endpoint returns error.
    /// </summary>
    public class DiscoveryEndpointException : Exception
    {
        /// <summary>Initializes a new instance of the <see cref="DiscoveryEndpointException" /> class.</summary>
        public DiscoveryEndpointException()
        {

        }

        /// <summary>Initializes a new instance of the <see cref="DiscoveryEndpointException" /> class with a specified error message.</summary>
        /// <param name="message">The message that describes the error.</param>
        public DiscoveryEndpointException(string message) : base(message)
        {

        }

        /// <summary>Initializes a new instance of the <see cref="DiscoveryEndpointException" /> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.</param>
        public DiscoveryEndpointException(string message, Exception? innerException) : base(message, innerException)
        {

        }
    }
}
