using System;
using System.IO;
using Microsoft.Extensions.Logging;

public class SemiE125Logger : ILogger
{
    private readonly string _categoryName;
    private readonly string _logFilePath;
    private static readonly object _lock = new object();

    public SemiE125Logger(string categoryName, string logFilePath)
    {
        _categoryName = categoryName;
        _logFilePath = logFilePath;
    }

    // 필수 메서드 1: 특정 로그 레벨이 활성화되어 있는지 확인
    public bool IsEnabled(LogLevel logLevel)
    {
        // 이 구현에서는 None을 제외한 모든 로그 레벨 허용
        return logLevel != LogLevel.None;
    }

    // 필수 메서드 2: 실제 로깅 수행
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {_categoryName}: {message}";

        if (exception != null)
        {
            logEntry += $"{Environment.NewLine}Exception: {exception}";
        }

        try
        {
            lock (_lock)
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            // 로그 쓰기 실패 시 콘솔에 출력 (개발 환경에서만)
            Console.WriteLine($"로그 작성 실패: {ex.Message}");
        }
    }

    // 필수 메서드 3: 로깅 범위 시작
    public IDisposable BeginScope<TState>(TState state)
    {
        // 기본 구현에서는 범위를 지원하지 않음
        return NullScope.Instance;
    }

    // 로깅 범위를 지원하지 않을 때 사용하는 내부 클래스
    private class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new NullScope();

        private NullScope() { }

        public void Dispose() { }
    }
}