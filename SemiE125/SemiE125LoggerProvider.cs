// SemiE125LoggerProvider.cs
using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace SemiE125.Logging
{
    public class SemiE125LoggerProvider : ILoggerProvider
    {
        private readonly string _logFilePath;

        public SemiE125LoggerProvider(string logFilePath)
        {
            _logFilePath = logFilePath;

            // 로그 디렉토리 확인 및 생성
            var logDir = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new SemiE125Logger(categoryName, _logFilePath);
        }

        public void Dispose()
        {
            // 필요한 정리 작업 수행
        }
    }
}