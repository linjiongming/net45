using System;
using System.Text;

namespace Microsoft.Extensions.Logging.Console
{
    // 配置选项类
    public class ConsoleLoggerOptions
    {
        public bool DisableColors { get; set; } = false;
        public string TimestampFormat { get; set; } = "HH:mm:ss.ffff";
        public LogLevel MinLevel { get; set; } = LogLevel.Trace;
    }

    // 日志提供程序实现
    public class ConsoleLoggerProvider : ILoggerProvider
    {
        private readonly ConsoleLoggerOptions _options;
        private readonly object _lock = new object();

        public ConsoleLoggerProvider(ConsoleLoggerOptions options)
        {
            _options = options ?? new ConsoleLoggerOptions();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new ConsoleLogger(categoryName, _options);
        }

        public void Dispose() { }
    }

    // 日志记录器实现
    public class ConsoleLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ConsoleLoggerOptions _options;

        public ConsoleLogger(string categoryName, ConsoleLoggerOptions options)
        {
            _categoryName = categoryName;
            _options = options;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            lock (_options) // 控制台输出线程安全 [[2]]
            {
                ConsoleColor originalColor = System.Console.ForegroundColor;

                try
                {
                    // 设置颜色
                    if (!_options.DisableColors)
                    {
                        System.Console.ForegroundColor = GetLogLevelColor(logLevel);
                    }

                    // 构建日志消息
                    string message = new StringBuilder()
                        .Append("[" + DateTime.Now.ToString(_options.TimestampFormat) + "] ")
                        .Append($"[{GetLogLevelAbbr(logLevel)}] ")
                        .Append($"[{_categoryName}] ")
                        .Append(formatter(state, exception))
                        .ToString();

                    System.Console.WriteLine(message);

                    // 输出异常信息
                    if (exception != null)
                    {
                        System.Console.WriteLine(exception.ToString());
                    }
                }
                finally
                {
                    System.Console.ForegroundColor = originalColor;
                }
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _options.MinLevel;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        private ConsoleColor GetLogLevelColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Critical: return ConsoleColor.Red;
                case LogLevel.Error: return ConsoleColor.DarkRed;
                case LogLevel.Warning: return ConsoleColor.Yellow;
                case LogLevel.Information: return ConsoleColor.White;
                case LogLevel.Debug: return ConsoleColor.Gray;
                case LogLevel.Trace: return ConsoleColor.DarkGray;
                default: return System.Console.ForegroundColor;
            }
        }

        private string GetLogLevelAbbr(LogLevel level)
        {
            string upper = level.ToString().ToUpper();
            switch (level)
            {
                default:
                case LogLevel.Critical:
                case LogLevel.Error:
                case LogLevel.Debug:
                case LogLevel.Trace:
                    return upper;
                case LogLevel.Warning:
                case LogLevel.Information:
                    return upper.Substring(0, 4);
            }
        }
    }

    // 扩展方法
    public static class ConsoleLoggerExtensions
    {
        public static ILoggerFactory AddConsole(
            this ILoggerFactory factory,
            Action<ConsoleLoggerOptions> configure = null)
        {
            ConsoleLoggerOptions options = new ConsoleLoggerOptions();
            configure?.Invoke(options);
            factory.AddProvider(new ConsoleLoggerProvider(options));
            return factory;
        }
    }
}