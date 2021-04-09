namespace Monq.Core.HttpClientExtensions
{
    /// <summary>
    /// Конфигурация базового http-клиента.
    /// </summary>
    public class BasicHttpServiceOptions
    {
        /// <summary>
        /// Конфигурация обработки http-заголовков.
        /// </summary>
        public BasicHttpServiceHeaderOptions RestHttpClientHeaderOptions { get; protected set; } = new BasicHttpServiceHeaderOptions();

        /// <summary>
        /// Выполнить настройку обработки заголовков запросов.
        /// </summary>
        /// <param name="options">Конфигурация обработки http-заголовков.</param>
        public void ConfigHeaders(BasicHttpServiceHeaderOptions options) =>
            RestHttpClientHeaderOptions = options;
    }
}
