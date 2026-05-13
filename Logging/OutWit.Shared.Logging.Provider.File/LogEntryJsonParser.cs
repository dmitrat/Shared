using System;
using System.Globalization;
using System.Text.Json;
using OutWit.Common.Logging.Query.Model;

namespace OutWit.Shared.Logging.Provider.File
{
    /// <summary>
    /// Parses a single NDJSON line written by Serilog's File sink into a neutral
    /// <see cref="LogEntry"/>. Defensive about field names — supports both the
    /// NewRelic enricher format used in the WitIdentity Docker image
    /// (<c>timestamp</c>/<c>log.level</c>/<c>message</c>) and Serilog's compact
    /// JSON shape (<c>@t</c>/<c>@l</c>/<c>@mt</c>) so the same plugin can be used
    /// across hosts with different formatter configurations.
    /// </summary>
    public static class LogEntryJsonParser
    {
        #region Functions

        /// <summary>
        /// Tries to parse the supplied line into a <see cref="LogEntry"/>.
        /// Returns <c>null</c> for blank lines, non-JSON lines, or JSON without a
        /// recognizable timestamp.
        /// </summary>
        public static LogEntry? TryParse(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(line);
            }
            catch (JsonException)
            {
                return null;
            }

            using (document)
            {
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                    return null;

                var timestamp = ReadTimestamp(document.RootElement);
                if (timestamp == null)
                    return null;

                return new LogEntry
                {
                    Timestamp = timestamp.Value,
                    Level = ReadLevel(document.RootElement),
                    Message = ReadString(document.RootElement, "message", "@m", "Message", "@mt", "MessageTemplate"),
                    Exception = ReadString(document.RootElement, "error.stack", "@x", "Exception"),
                    SourceContext = ReadSourceContext(document.RootElement),
                    ServiceName = ReadString(document.RootElement, "service.name", "ServiceName", "serviceName"),
                    Host = ReadString(document.RootElement, "hostname", "host.name", "Host", "Hostname"),
                    Environment = ReadString(document.RootElement, "environment", "env"),
                    TraceId = ReadString(document.RootElement, "trace.id", "TraceId", "traceId"),
                    SpanId = ReadString(document.RootElement, "span.id", "SpanId", "spanId")
                };
            }
        }

        #endregion

        #region Tools

        private static DateTime? ReadTimestamp(JsonElement obj)
        {
            foreach (var key in new[] { "timestamp", "@t", "Timestamp", "@timestamp" })
            {
                if (!obj.TryGetProperty(key, out var element))
                    continue;

                switch (element.ValueKind)
                {
                    case JsonValueKind.String:
                        if (DateTime.TryParse(element.GetString(), CultureInfo.InvariantCulture,
                                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var iso))
                            return iso;
                        break;

                    case JsonValueKind.Number:
                        // NewRelic timestamps are Unix milliseconds.
                        if (element.TryGetInt64(out var unixMs))
                            return DateTimeOffset.FromUnixTimeMilliseconds(unixMs).UtcDateTime;
                        break;
                }
            }
            return null;
        }

        private static LogSeverity? ReadLevel(JsonElement obj)
        {
            foreach (var key in new[] { "log.level", "@l", "Level", "level" })
            {
                if (!obj.TryGetProperty(key, out var element))
                    continue;

                var raw = element.ValueKind == JsonValueKind.String ? element.GetString() : null;
                if (string.IsNullOrEmpty(raw))
                    continue;

                return NormalizeLevel(raw);
            }
            // Serilog compact JSON omits Level for Information.
            return LogSeverity.Information;
        }

        private static LogSeverity? NormalizeLevel(string raw)
        {
            // Accept Serilog full names ("Information"), short forms ("Info"),
            // and lowercase NR-style ("warn", "info").
            switch (raw.Trim().ToLowerInvariant())
            {
                case "trace":       return LogSeverity.Trace;
                case "debug":       return LogSeverity.Debug;
                case "info":
                case "information": return LogSeverity.Information;
                case "warn":
                case "warning":     return LogSeverity.Warning;
                case "error":       return LogSeverity.Error;
                case "critical":    return LogSeverity.Critical;
                case "fatal":       return LogSeverity.Fatal;
                default:            return null;
            }
        }

        private static string? ReadString(JsonElement obj, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (obj.TryGetProperty(key, out var element) && element.ValueKind == JsonValueKind.String)
                {
                    var value = element.GetString();
                    if (!string.IsNullOrEmpty(value))
                        return value;
                }
            }
            return null;
        }

        private static string? ReadSourceContext(JsonElement obj)
        {
            // Direct keys first (NR-style, compact Serilog with renderings).
            var direct = ReadString(obj, "logger.name", "SourceContext", "logger");
            if (!string.IsNullOrEmpty(direct))
                return direct;

            // Then dive into the Serilog Properties bag.
            if (obj.TryGetProperty("Properties", out var props) && props.ValueKind == JsonValueKind.Object)
                return ReadString(props, "SourceContext", "logger.name");

            return null;
        }

        #endregion
    }
}
