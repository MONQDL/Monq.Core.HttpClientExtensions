﻿namespace Monq.Core.HttpClientExtensions.Extensions
{
    /// <summary>
    /// The class provides a set of wrapper methods over the <see cref="RestHttpClientSerializer"/>.
    /// </summary>
    static class JsonExtensions
    {
        /// <summary>
        /// Deserialize the JSON string into an object of type <typeparamref name = "T" />.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize to.</typeparam>
        /// <param name="value">A string with the data to be deserialized. 
        /// If string is empty, than default(T) will be returned.</param>
        public static T? JsonToObject<T>(this string? value)
        {
            if (string.IsNullOrEmpty(value))
                return default(T);
            return RestHttpClientSerializer.Deserialize<T>(value);
        }
    }
}
