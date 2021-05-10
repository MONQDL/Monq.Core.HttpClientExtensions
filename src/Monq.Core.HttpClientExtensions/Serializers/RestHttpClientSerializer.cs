using System;

namespace Monq.Core.HttpClientExtensions
{
    internal static class RestHttpClientSerializer
    {
        static IRestHttpClientSerializer _currentSerializer = new RestHttpClientNewtonsoftJsonSerializer();

        /// <summary>
        /// Serialize object to JSON string using the System.Text.Json or Newtonsoft.Json serializer.
        /// </summary>
        /// <typeparam name="TValue">The type of the serialized object.</typeparam>
        /// <param name="value">The value that must be serialized.</param>
        /// <returns></returns>
        public static string Serialize<TValue>(TValue value)
        {
            return _currentSerializer.Serialize(value);
        }

        /// <summary>
        /// Deserialize string to object using the System.Text.Json or Newtonsoft.Json serializer.
        /// </summary>
        /// <typeparam name="TResult">The type of the result object.</typeparam>
        /// <param name="value">The string value containing the JSON.</param>
        /// <returns></returns>
        public static TResult? Deserialize<TResult>(string value)
        {
            return _currentSerializer.Deserialize<TResult>(value);
        }

        public static void UseSystemTextJson(Action<System.Text.Json.JsonSerializerOptions>? setupAction = null)
        {
            _currentSerializer = new RestHttpClientSystemTextJsonSerializer(setupAction);
        }

        public static void UseNewtonsoftJson(Action<Newtonsoft.Json.JsonSerializerSettings>? setupAction = null)
        {
            _currentSerializer = new RestHttpClientNewtonsoftJsonSerializer(setupAction);
        }
    }
}
