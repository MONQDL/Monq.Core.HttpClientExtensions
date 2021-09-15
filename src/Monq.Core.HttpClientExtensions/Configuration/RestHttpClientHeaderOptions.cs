using System.Collections.Generic;

namespace Monq.Core.HttpClientExtensions
{
    /// <summary>
    /// Конфигурация обработки http-заголовков.
    /// </summary>
    public class RestHttpClientHeaderOptions
    {
        /// <summary>
        /// Список названий заголовков, которые будут проброшены на нижестоящий сервис при вызове методов HttpClientExtensions.
        /// </summary>
        public HashSet<string> ForwardedHeaders { get; set; } = new HashSet<string>();

        /// <summary>
        /// Флаг устанавливает поведение, при котором значения заголовков, пробрасываемые через <see cref="ForwardedHeaders"/> будет отображено в логах.
        /// По умолчанию true.
        /// </summary>
        public bool LogForwardedHeaders { get; set; } = true;

        /// <summary>
        /// Добавить заголовок в список заголовков, которые будут проброшены на нижестоящий сервис при вызове методов HttpClientExtensions.
        /// </summary>
        /// <param name="header">Название заголовка.</param>
        public RestHttpClientHeaderOptions AddForwardedHeader(string header)
        {
            if (!ForwardedHeaders.Contains(header))
                ForwardedHeaders.Add(header);

            return this;
        }
    }
}