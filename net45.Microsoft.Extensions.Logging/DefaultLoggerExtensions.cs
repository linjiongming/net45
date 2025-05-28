using System;
using System.Collections.Concurrent;

namespace Microsoft.Extensions.Logging
{
    public static class DefaultLoggerExtensions
    {
        private static readonly ConcurrentDictionary<string, EventId> _eventIdCache = new ConcurrentDictionary<string, EventId>();
        private static readonly EventId _defaultEventId = new EventId(0, "Default");

        public static void LogError(this ILogger logger, Exception exception, string message, params object[] args)
        {
            var name = exception?.TargetSite?.DeclaringType?.FullName;
            var eventId = string.IsNullOrWhiteSpace(name) ? _defaultEventId : _eventIdCache.GetOrAdd(name, n => new EventId(n.GetHashCode(), n));
            logger.LogError(eventId, exception, message, args);
        }
    }
}