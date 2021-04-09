using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Monq.Core.HttpClientExtensions.Tests.Stubs
{
    public class StubLogger : ILogger
    {
        public List<string> LoggingEvents { get; set; } = new List<string>();

        public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            LoggingEvents.Add(state.ToString());
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public IDisposable? BeginScopeImpl(object state)
        {
            return null;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            LoggingEvents.Add(state.ToString());
        }

        public IDisposable? BeginScope<TState>(TState state)
        {
            return null;
        }
    }

    public class StubLogger<T> : StubLogger, ILogger<T>
    {
    }
}
