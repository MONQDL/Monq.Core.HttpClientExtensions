using System;

namespace Monq.Core.HttpClientExtensions
{
    /// <summary>
    /// The serialization factory that uses be the RestHttpClient to serialize/deserialize requests and responses.
    /// </summary>
    public interface IRestHttpClientSerializer
    {
        /// <summary>
        /// Serialize the value <paramref name="value"/> of type <typeparamref name="TValue"/> to string.
        /// </summary>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="value">The object to serialize.</param>
        /// <returns>Serialized string.</returns>
        string Serialize<TValue>(TValue value);

        /// <summary>
        /// Deserialize the value <paramref name="value"/> from string to <typeparamref name="TResult"/> type.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="value">The serialized string.</param>
        /// <returns>The object of type <typeparamref name="TResult"/> or null.</returns>
        TResult? Deserialize<TResult>(string value);
    }
}
