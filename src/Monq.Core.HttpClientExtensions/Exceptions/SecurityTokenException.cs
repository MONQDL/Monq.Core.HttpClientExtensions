using System;

namespace Monq.Core.HttpClientExtensions.Exceptions
{
#pragma warning disable CS1591
    public class SecurityTokenException : Exception
    {
        public SecurityTokenException()
        {
        }

        public SecurityTokenException(string message)
            : base(message)
        {
        }

        public SecurityTokenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
#pragma warning restore CS1591
}
