using System;

namespace Monq.Core.HttpClientExtensions
{
    public class RestHttpClientSystemTextJsonSerializer : IRestHttpClientSerializer
    {
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

        public string Serialize<TValue>(TValue value)
        {
            return System.Text.Json.JsonSerializer.Serialize(value, Options);
        }

        public TResult? Deserialize<TResult>(string value)
        {
            return System.Text.Json.JsonSerializer.Deserialize<TResult>(value, Options);
        }
    }
}
