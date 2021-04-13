using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Monq.Core.HttpClientExtensions.Tests.Stubs
{
    public class StubLoggerFactory : ILoggerFactory
    {
        readonly IList<StubLogger> _loggers;

        public StubLoggerFactory(IList<StubLogger> loggers)
        {
            _loggers = loggers;
        }

        public void AddProvider(ILoggerProvider provider)
        {
            throw new NotImplementedException();
        }

        public ILogger<T> CreateLog<T>()
        {
            var logger = new StubLogger<T>();
            _loggers.Add(logger);
            return logger;
        }

        public ILogger CreateLogger(string categoryName)
        {
            var logger = new StubLogger();
            _loggers.Add(logger);
            return logger;
        }

        public void Dispose()
        {
        }
    }
}
