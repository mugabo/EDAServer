using System;
using Microsoft.Extensions.Logging;

namespace SemiE125.Logging
{
    public class ConsoleLogger<T> : ILogger<T>
    {
        private readonly Microsoft.Extensions.Logging.LogLevel _minLevel;
        private readonly string _name;

        public ConsoleLogger(Microsoft.Extensions.Logging.LogLevel minLevel)
        {
            _minLevel = minLevel;
            _name = typeof(T).Name;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return logLevel >= _minLevel;
        }

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var color = System.Console.ForegroundColor;
            switch (logLevel)
            {
                case Microsoft.Extensions.Logging.LogLevel.Critical:
                case Microsoft.Extensions.Logging.LogLevel.Error:
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Warning:
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Information:
                    System.Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Debug:
                case Microsoft.Extensions.Logging.LogLevel.Trace:
                    System.Console.ForegroundColor = ConsoleColor.Gray;
                    break;
            }

            var message = formatter(state, exception);
            System.Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel}] [{_name}] {message}");

            if (exception != null)
            {
                System.Console.WriteLine($"Exception: {exception}");
            }

            System.Console.ForegroundColor = color;
        }

        // NullScope 내부 클래스
        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();

            private NullScope() { }

            public void Dispose() { }
        }
    }
}