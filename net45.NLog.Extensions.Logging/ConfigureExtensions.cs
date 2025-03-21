using Microsoft.Extensions.Logging;

namespace NLog.Extensions.Logging
{
    /// <summary>
    /// Helpers for configuring NLog for Microsoft Extension Logging (MEL)
    /// </summary>
    public static class ConfigureExtensions
    {
        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging
        /// </summary>
        /// <param name="factory"></param>
        /// <returns>ILoggerFactory for chaining</returns>
        public static ILoggerFactory AddNLog(this ILoggerFactory factory)
        {
            return factory.AddNLog(NLogProviderOptions.Default);
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="options">NLog Logging Provider options</param>
        /// <returns>ILoggerFactory for chaining</returns>
        public static ILoggerFactory AddNLog(this ILoggerFactory factory, NLogProviderOptions options)
        {
            factory.AddProvider(new NLogLoggerProvider(options));
            return factory;
        }
    }
}
