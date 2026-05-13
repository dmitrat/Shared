using System;
using System.Linq;
using OutWit.Common.Logging.Query.Model;

namespace OutWit.Shared.Logging.Provider.File
{
    /// <summary>
    /// Evaluates a <see cref="LogFilter"/> against a neutral <see cref="LogEntry"/>
    /// — the in-memory equivalent of what NerdGraph / Loki / Elasticsearch do
    /// server-side. Used by <see cref="FileLogQueryProvider"/> to apply user-supplied
    /// filters after parsing the JSON files.
    /// </summary>
    public static class LogFilterMatcher
    {
        #region Functions

        /// <summary>
        /// Returns <c>true</c> when every supplied filter matches the entry.
        /// </summary>
        public static bool Matches(LogEntry entry, LogFilter[]? filters)
        {
            if (filters == null || filters.Length == 0)
                return true;

            foreach (var filter in filters)
            {
                if (!MatchesOne(entry, filter))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Returns <c>true</c> when the entry's <see cref="LogEntry.Message"/> /
        /// <see cref="LogEntry.Exception"/> contains the substring (case-insensitive).
        /// Mirrors the full-text search semantics most log backends offer.
        /// </summary>
        public static bool MatchesFreeText(LogEntry entry, string? freeText)
        {
            if (string.IsNullOrWhiteSpace(freeText))
                return true;

            return Contains(entry.Message, freeText)
                || Contains(entry.Exception, freeText);
        }

        #endregion

        #region Tools

        private static bool MatchesOne(LogEntry entry, LogFilter filter)
        {
            var value = ReadAttribute(entry, filter.Attribute);

            switch (filter.Operator)
            {
                case LogFilterOperator.Equals:
                    return filter.Values.Length > 0 && string.Equals(value, filter.Values[0], StringComparison.OrdinalIgnoreCase);

                case LogFilterOperator.NotEquals:
                    return filter.Values.Length > 0 && !string.Equals(value, filter.Values[0], StringComparison.OrdinalIgnoreCase);

                case LogFilterOperator.Contains:
                    return filter.Values.Length > 0 && Contains(value, filter.Values[0]);

                case LogFilterOperator.NotContains:
                    return filter.Values.Length > 0 && !Contains(value, filter.Values[0]);

                case LogFilterOperator.In:
                    return filter.Values.Any(v => string.Equals(value, v, StringComparison.OrdinalIgnoreCase));

                case LogFilterOperator.GreaterThan:
                case LogFilterOperator.GreaterOrEqual:
                case LogFilterOperator.LessThan:
                case LogFilterOperator.LessOrEqual:
                    return CompareNumeric(value, filter);

                default:
                    return false;
            }
        }

        private static bool Contains(string? haystack, string needle)
        {
            return haystack != null
                   && haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string? ReadAttribute(LogEntry entry, string attributeName)
        {
            if (string.IsNullOrEmpty(attributeName))
                return null;

            if (LogAttribute.Level.Is(attributeName))         return entry.Level?.Value;
            if (LogAttribute.Message.Is(attributeName))       return entry.Message;
            if (LogAttribute.Exception.Is(attributeName))     return entry.Exception;
            if (LogAttribute.SourceContext.Is(attributeName)) return entry.SourceContext;
            if (LogAttribute.ServiceName.Is(attributeName))   return entry.ServiceName;
            if (LogAttribute.Host.Is(attributeName))          return entry.Host;
            if (LogAttribute.Environment.Is(attributeName))   return entry.Environment;
            if (LogAttribute.TraceId.Is(attributeName))       return entry.TraceId;
            if (LogAttribute.SpanId.Is(attributeName))        return entry.SpanId;
            if (LogAttribute.Timestamp.Is(attributeName))     return entry.Timestamp.ToString("O");

            return null;
        }

        private static bool CompareNumeric(string? value, LogFilter filter)
        {
            if (filter.Values.Length == 0)
                return false;

            if (!double.TryParse(value, out var left) ||
                !double.TryParse(filter.Values[0], out var right))
                return false;

            return filter.Operator switch
            {
                LogFilterOperator.GreaterThan    => left > right,
                LogFilterOperator.GreaterOrEqual => left >= right,
                LogFilterOperator.LessThan       => left < right,
                LogFilterOperator.LessOrEqual    => left <= right,
                _                                => false
            };
        }

        #endregion
    }
}
