namespace Monq.Core.HttpClientExtensions
{
    /// <summary>
    /// A set of constant values for serving Http requests.
    /// </summary>
    public static class TraceConstants
    {
        /// <summary>
        /// EventId in ASP.NET for defining the event type "Request to the downstream service" in the logging system.
        /// </summary>
        public const int DownServiceEventId = 2501;
    }
}
