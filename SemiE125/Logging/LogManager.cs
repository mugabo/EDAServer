// Logging/LogManager.cs
using System;
using System.IO;
using System.Threading.Tasks;

namespace SemiE125.Logging
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public static class LogManager
    {
        private static readonly object _lock = new object();
        private static string _logFilePath = "e125_log.txt";
        private static LogLevel _minLogLevel = LogLevel.Info;

        public static void Configure(string logFilePath, LogLevel minLogLevel)
        {
            _logFilePath = logFilePath;
            _minLogLevel = minLogLevel;
        }

        public static void Log(LogLevel level, string message)
        {
            if (level < _minLogLevel)
                return;

            var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";

            try
            {
                lock (_lock)
                {
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }

                // 콘솔에도 출력
                Console.WriteLine(logEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"로깅 오류: {ex.Message}");
            }
        }

        public static void Debug(string message) => Log(LogLevel.Debug, message);
        public static void Info(string message) => Log(LogLevel.Info, message);
        public static void Warning(string message) => Log(LogLevel.Warning, message);
        public static void Error(string message) => Log(LogLevel.Error, message);
        public static void Critical(string message) => Log(LogLevel.Critical, message);

        public static void LogException(Exception ex, string context = null)
        {
            var message = string.IsNullOrEmpty(context)
                ? $"예외 발생: {ex.Message}"
                : $"예외 발생 in {context}: {ex.Message}";

            Log(LogLevel.Error, message);
            Log(LogLevel.Debug, $"스택 트레이스: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                Log(LogLevel.Debug, $"내부 예외: {ex.InnerException.Message}");
            }
        }
    }
}