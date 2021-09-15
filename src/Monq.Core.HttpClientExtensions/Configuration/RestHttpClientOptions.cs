namespace Monq.Core.HttpClientExtensions
{
    /// <summary>
    /// Конфигурация базового http-клиента.
    /// </summary>
    public class RestHttpClientOptions
    {
        /// <summary>
        /// Конфигурация обработки http-заголовков.
        /// </summary>
        public RestHttpClientHeaderOptions RestHttpClientHeaderOptions { get; protected set; } = new RestHttpClientHeaderOptions();

        /// <summary>
        /// Выполнить настройку обработки заголовков запросов.
        /// </summary>
        /// <param name="options">Конфигурация обработки http-заголовков.</param>
        public void ConfigHeaders(RestHttpClientHeaderOptions options) =>
            RestHttpClientHeaderOptions = options;
    }
}
