using System;
using System.Diagnostics.CodeAnalysis;

namespace Monq.Core.HttpClientExtensions
{
    [RequiresUnreferencedCode("This class uses Newtonsoft.Json which is not compatible with trimming. Consider using System.Text.Json instead.")]
    internal static class RestHttpClientSerializer
    {
        static IRestHttpClientSerializer _currentSerializer = new RestHttpClientNewtonsoftJsonSerializer();

        /// <summary>
        /// Get current configured serializer.
        /// </summary>
        public static IRestHttpClientSerializer CurrentSerializer { get => _currentSerializer; }

        /// <summary>
        /// Serialize object to JSON string using the System.Text.Json or Newtonsoft.Json serializer.
        /// </summary>
        /// <typeparam name="TValue">The type of the serialized object.</typeparam>
        /// <param name="value">The value that must be serialized.</param>
        /// <returns></returns>
        [RequiresUnreferencedCode(
            "Newtonsoft.Json.JsonConvert.DeserializeObject and System.Text.Json.JsonSerializer is incompatible with trimming.")]
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
        [RequiresUnreferencedCode(
            "Newtonsoft.Json.JsonConvert.DeserializeObject and System.Text.Json.JsonSerializer is incompatible with trimming.")]
        public static TResult? Deserialize<TResult>(string value)
        {
            return _currentSerializer.Deserialize<TResult>(value);
        }

        /// <summary>
        /// Use System.Text.Json serializer as default serializer for the HttpClient requests and responses.
        /// </summary>
        /// <param name="setupAction"></param>
        public static void UseSystemTextJson(Action<System.Text.Json.JsonSerializerOptions>? setupAction = null)
        {
            _currentSerializer = new RestHttpClientSystemTextJsonSerializer(setupAction);
        }

        /// <summary>
        /// Use NewtonsoftJson serializer as default serializer for the HttpClient requests and responses.
        /// </summary>
        /// <param name="setupAction"></param>
        [RequiresUnreferencedCode(
            "Serializers is incompatible with trimming.")]
        public static void UseNewtonsoftJson(Action<Newtonsoft.Json.JsonSerializerSettings>? setupAction = null)
        {
            _currentSerializer = new RestHttpClientNewtonsoftJsonSerializer(setupAction);
        }
    }
}
