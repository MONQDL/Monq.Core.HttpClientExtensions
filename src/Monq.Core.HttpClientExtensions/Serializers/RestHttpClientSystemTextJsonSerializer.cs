using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Monq.Core.HttpClientExtensions
{
    /// <summary>
    /// System.Text.Json serializer for RestHttpClient.
    /// </summary>
    public class RestHttpClientSystemTextJsonSerializer : IRestHttpClientSerializer
    {
        static RestHttpClientSystemTextJsonSerializer _default = new RestHttpClientSystemTextJsonSerializer();

        /// <summary>
        /// The singleton instance with default options.
        /// </summary>
        public static RestHttpClientSystemTextJsonSerializer Default => _default;

        /// <summary>
        /// Serializer options.
        /// </summary>
        public System.Text.Json.JsonSerializerOptions Options { get; }

        /// <summary>
        /// Creates new object of <see cref="RestHttpClientSystemTextJsonSerializer"/>.
        /// </summary>
        /// <param name="setupAction">Custom configuration of the serializer.</param>
        public RestHttpClientSystemTextJsonSerializer(Action<System.Text.Json.JsonSerializerOptions>? setupAction = null)
        {
            if (setupAction is null)
            {
                Options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };
                Options.Converters.Add(new JsonStringEnumConverter());
            }
            else
            {
                Options = new System.Text.Json.JsonSerializerOptions();
                setupAction(Options);
            }
        }

        /// <inheritdoc />
        [RequiresUnreferencedCode(
            "System.Text.Json.JsonSerializer.Deserialize is incompatible with trimming.")]
        public string Serialize<TValue>(TValue value)
        {
            return System.Text.Json.JsonSerializer.Serialize(value, Options);
        }

        /// <inheritdoc />
        [RequiresUnreferencedCode(
            "System.Text.Json.JsonSerializer.Deserialize is incompatible with trimming.")]
        public TResult? Deserialize<TResult>(string value)
        {
            return System.Text.Json.JsonSerializer.Deserialize<TResult>(value, Options);
        }
    }
}
