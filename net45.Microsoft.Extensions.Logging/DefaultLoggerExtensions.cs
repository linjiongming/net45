using System;

namespace Microsoft.Extensions.Logging
{
    public static class DefaultLoggerExtensions
    {
        public static readonly EventId DefaultEventId = new EventId(0, "Default");

        public static void LogError(this ILogger logger, Exception exception, string message, params object[] args)
        {
            logger.LogError(DefaultEventId, exception, message, args);
        }
    }
}
