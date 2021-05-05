using System;

namespace Monq.Core.HttpClientExtensions
{
    public interface IRestHttpClientSerializer
    {
        string Serialize<TValue>(TValue value);
        TResult? Deserialize<TResult>(string value);
    }
}
