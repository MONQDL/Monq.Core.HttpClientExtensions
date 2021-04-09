using System;
using System.Runtime.Serialization;

namespace Monq.Core.HttpClientExtensions.Exceptions
{
#pragma warning disable CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
    public class MissingConfigurationException : Exception
    {
        public MissingConfigurationException()
        {
        }

        public MissingConfigurationException(string message) : base(message)
        {
        }

        public MissingConfigurationException(string message, Exception? innerException) : base(message, innerException)
        {
        }

        protected MissingConfigurationException(SerializationInfo info, in StreamingContext context) : base(info, context)
        {
        }
    }
}
