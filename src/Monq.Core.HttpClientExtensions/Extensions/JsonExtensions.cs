namespace Monq.Core.HttpClientExtensions.Extensions
{
    /// <summary>
    /// Класс предоставляет набор методов-оберток над сериализатором Newtonsoft.Json.
    /// </summary>
    static class JsonExtensions
    {
        /// <summary>
        /// Метод выполняет десериализацию строки JSON в объект типа <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Тип объекта, в который требуется провести десериализацию.</typeparam>
        /// <param name="value">Строка с данными, которые требуется десериализовать.</param>
        public static T? JsonToObject<T>(this string? value)
        {
            if (string.IsNullOrEmpty(value))
                return default(T);
            return RestHttpClientSerializer.Deserialize<T>(value);
        }
    }
}
