using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace userspace_backend.Logging
{
    public sealed class FileLoggerProvider : ILoggerProvider
    {
        private readonly string filePath;
        private readonly ConcurrentDictionary<string, FileLogger> loggers = new();
        private readonly object writeLock = new();

        public FileLoggerProvider(string filePath)
        {
            this.filePath = filePath;
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public ILogger CreateLogger(string categoryName)
            => loggers.GetOrAdd(categoryName, name => new FileLogger(name, this));

        public void Dispose() => loggers.Clear();

        internal void Append(string categoryName, LogLevel level, string message, Exception? exception)
        {
            var sb = new StringBuilder();
            sb.Append('[').Append(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fffZ")).Append("] ");
            sb.Append('[').Append(level).Append("] ");
            sb.Append('[').Append(categoryName).Append("] ");
            sb.Append(message);
            if (exception != null)
            {
                sb.AppendLine();
                sb.Append(exception);
            }
            sb.AppendLine();

            lock (writeLock)
            {
                try
                {
                    File.AppendAllText(filePath, sb.ToString(), Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    // Never crash the app because of a log write failure, but make the
                    // failure observable instead of silently losing log output.
                    Debug.WriteLine($"FileLoggerProvider failed to write to '{filePath}': {ex}");
                }
            }
        }

        private sealed class FileLogger : ILogger
        {
            private readonly string categoryName;
            private readonly FileLoggerProvider owner;

            public FileLogger(string categoryName, FileLoggerProvider owner)
            {
                this.categoryName = categoryName;
                this.owner = owner;
            }

            IDisposable ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                var message = formatter(state, exception);
                owner.Append(categoryName, logLevel, message, exception);
            }

            private sealed class NullScope : IDisposable
            {
                public static readonly NullScope Instance = new();
                public void Dispose() { }
            }
        }
    }
}
