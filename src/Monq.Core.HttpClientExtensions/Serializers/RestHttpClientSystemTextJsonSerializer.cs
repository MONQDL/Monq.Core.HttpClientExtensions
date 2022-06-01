using System;

namespace Monq.Core.HttpClientExtensions
{
    public class RestHttpClientSystemTextJsonSerializer : IRestHttpClientSerializer
    {
        static RestHttpClientSystemTextJsonSerializer _default = new RestHttpClientSystemTextJsonSerializer();

        /// <summary>
        /// The singleton instance with default options.
        /// </summary>
        public static RestHttpClientSystemTextJsonSerializer Default => _default;

        public System.Text.Json.JsonSerializerOptions Options { get; }

        public RestHttpClientSystemTextJsonSerializer(Action<System.Text.Json.JsonSerializerOptions>? setupAction = null)
        {
            if (setupAction is null)
                Options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };
            else
            {
                Options = new System.Text.Json.JsonSerializerOptions();
                setupAction(Options);
            }
        }

        /// <inheritdoc />
        public string Serialize<TValue>(TValue value)
        {
            return System.Text.Json.JsonSerializer.Serialize(value, Options);
        }

        /// <inheritdoc />
        public TResult? Deserialize<TResult>(string value)
        {
            return System.Text.Json.JsonSerializer.Deserialize<TResult>(value, Options);
        }
    }
}
