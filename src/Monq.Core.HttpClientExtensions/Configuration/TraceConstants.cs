namespace Monq.Core.HttpClientExtensions
{
    /// <summary>
    /// Набор константных значений для обслуживания Http запросов.
    /// </summary>
    public static class TraceConstants
    {
        /// <summary>
        /// EventId в ASP.NET для определения типа события "Запрос к нижестоящему сервису" в системе логирования.
        /// </summary>
        public const int DownServiceEventId = 2501;
    }
}
