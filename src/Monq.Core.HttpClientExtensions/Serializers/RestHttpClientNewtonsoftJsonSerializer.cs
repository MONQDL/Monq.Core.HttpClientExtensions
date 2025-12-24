using System;
using System.Diagnostics.CodeAnalysis;

namespace Monq.Core.HttpClientExtensions
{
    /// <summary>
    /// NewtonsoftJson serializer for RestHttpClient.
    /// </summary>
    [RequiresUnreferencedCode("This class uses Newtonsoft.Json which is not compatible with trimming. Consider using System.Text.Json instead.")]
    public class RestHttpClientNewtonsoftJsonSerializer : IRestHttpClientSerializer
    {
        static RestHttpClientNewtonsoftJsonSerializer _default = new RestHttpClientNewtonsoftJsonSerializer();

        /// <summary>
        /// The singleton instance with default options.
        /// </summary>
        public static RestHttpClientNewtonsoftJsonSerializer Default => _default;

        /// <summary>
        /// Serializer options.
        /// </summary>
        public Newtonsoft.Json.JsonSerializerSettings Options { get; }

        static readonly Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver _jsonResolver =
            new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver
            {
                NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = true
                }
            };

        /// <summary>
        /// Creates new object of <see cref="RestHttpClientNewtonsoftJsonSerializer"/>.
        /// </summary>
        /// <param name="setupAction">Custom configuration of the serializer.</param>
        public RestHttpClientNewtonsoftJsonSerializer(Action<Newtonsoft.Json.JsonSerializerSettings>? setupAction = null)
        {
            if (setupAction is null)
            {
                Options = new Newtonsoft.Json.JsonSerializerSettings() { ContractResolver = _jsonResolver };
            }
            else
            {
                Options = new Newtonsoft.Json.JsonSerializerSettings();
                setupAction(Options);
            }
        }

        /// <inheritdoc />
        [RequiresUnreferencedCode(
            "Newtonsoft.Json.JsonConvert.SerializeObject is incompatible with trimming.")]
        public string Serialize<TValue>(TValue value)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(value, Options);
        }

        /// <inheritdoc />
        [RequiresUnreferencedCode(
            "Newtonsoft.Json.JsonConvert.DeserializeObject is incompatible with trimming.")]
        public TResult? Deserialize<TResult>(string value)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<TResult>(value, Options);
        }
    }
}
