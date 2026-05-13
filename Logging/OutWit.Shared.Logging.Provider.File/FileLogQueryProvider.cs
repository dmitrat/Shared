using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Common.Logging.Query;
using OutWit.Common.Logging.Query.Model;

namespace OutWit.Shared.Logging.Provider.File
{
    /// <summary>
    /// <see cref="ILogQueryProvider"/> backed by Serilog-style NDJSON files on disk.
    /// Scans the host's log directory, parses each line into a neutral
    /// <see cref="LogEntry"/>, filters in-memory, sorts, and pages.
    /// </summary>
    /// <remarks>
    /// Honest limitations (documented in README): only sees this host's logs,
    /// O(scan) per query (capped by <see cref="FileLogProviderOptions.MaxScannedEntries"/>),
    /// loses old logs on Serilog rotation, no live tail. Suitable for
    /// single-instance deployments, air-gapped installs, dev, and disaster
    /// fallback if the configured log backend is unreachable.
    /// </remarks>
    public sealed class FileLogQueryProvider : ILogQueryProvider
    {
        #region Constants

        private const int DEFAULT_PAGE_SIZE = 200;

        #endregion

        #region Constructors

        public FileLogQueryProvider(string logsPath, string filePattern, FileLogProviderOptions options, ILogger<FileLogQueryProvider> logger)
        {
            m_logsPath = logsPath ?? throw new ArgumentNullException(nameof(logsPath));
            m_filePattern = filePattern ?? throw new ArgumentNullException(nameof(filePattern));
            m_options = options ?? throw new ArgumentNullException(nameof(options));
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Fields

        private readonly string m_logsPath;

        private readonly string m_filePattern;

        private readonly FileLogProviderOptions m_options;

        private readonly ILogger<FileLogQueryProvider> m_logger;

        #endregion

        #region ILogQueryProvider

        public Task<LogPage> QueryAsync(LogQuery query, CancellationToken cancellationToken = default)
        {
            if (query is null) throw new ArgumentNullException(nameof(query));

            var (from, to) = ResolveRange(query);
            var pageSize = query.PageSize ?? DEFAULT_PAGE_SIZE;
            var offset = query.Offset;
            var sort = query.SortOrder;

            var matching = LoadMatching(from, to, query.Filters, query.FullTextSearch, cancellationToken);

            var sorted = sort == LogSortOrder.Ascending
                ? matching.OrderBy(e => e.Timestamp)
                : (IEnumerable<LogEntry>)matching.OrderByDescending(e => e.Timestamp);

            var paged = sorted.Skip(offset).Take(pageSize + 1).ToList();
            var hasMore = paged.Count > pageSize;
            var items = paged.Take(pageSize).ToArray();

            return Task.FromResult(new LogPage
            {
                Offset = offset,
                PageSize = pageSize,
                HasMore = hasMore,
                Items = items
            });
        }

        public Task<LogPage> GetLogsAsync(DateTime from, DateTime to, IReadOnlyList<LogFilter>? filters = null, int? pageSize = null, int offset = 0, CancellationToken cancellationToken = default)
        {
            var query = new LogQuery
            {
                From = from,
                To = to,
                Filters = filters?.ToArray(),
                PageSize = pageSize,
                Offset = offset
            };
            return QueryAsync(query, cancellationToken);
        }

        public Task<LogPage> GetRecentLogsAsync(TimeSpan lookback, IReadOnlyList<LogFilter>? filters = null, int? pageSize = null, int offset = 0, CancellationToken cancellationToken = default)
        {
            var query = new LogQuery
            {
                Lookback = lookback,
                Filters = filters?.ToArray(),
                PageSize = pageSize,
                Offset = offset
            };
            return QueryAsync(query, cancellationToken);
        }

        public Task<LogPage> SearchAsync(string text, TimeSpan lookback, IReadOnlyList<LogFilter>? extraFilters = null, int? pageSize = null, int offset = 0, CancellationToken cancellationToken = default)
        {
            var query = new LogQuery
            {
                Lookback = lookback,
                FullTextSearch = text,
                Filters = extraFilters?.ToArray(),
                PageSize = pageSize,
                Offset = offset
            };
            return QueryAsync(query, cancellationToken);
        }

        public Task<IReadOnlyList<string>> GetDistinctValuesAsync(DateTime from, DateTime to, LogAttribute attribute, IReadOnlyList<LogFilter>? filters = null, int limit = 1000, CancellationToken cancellationToken = default)
        {
            var matching = LoadMatching(from, to, filters?.ToArray(), freeText: null, cancellationToken);

            IReadOnlyList<string> result = matching
                .Select(e => Read(e, attribute))
                .Where(v => !string.IsNullOrEmpty(v))
                .Select(v => v!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(limit)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<long> FindOffsetAsync(LogQuery query, DateTime timestamp, CancellationToken cancellationToken = default)
        {
            if (query is null) throw new ArgumentNullException(nameof(query));

            var (from, to) = ResolveRange(query);
            var matching = LoadMatching(from, to, query.Filters, query.FullTextSearch, cancellationToken);

            var sorted = query.SortOrder == LogSortOrder.Ascending
                ? matching.OrderBy(e => e.Timestamp).ToList()
                : matching.OrderByDescending(e => e.Timestamp).ToList();

            long offset = 0;
            foreach (var entry in sorted)
            {
                if (query.SortOrder == LogSortOrder.Descending
                    ? entry.Timestamp <= timestamp
                    : entry.Timestamp >= timestamp)
                {
                    return Task.FromResult(offset);
                }
                offset++;
            }
            return Task.FromResult(offset);
        }

        public Task<LogStatistics> GetStatisticsAsync(DateTime from, DateTime to, IReadOnlyList<LogFilter>? filters = null, CancellationToken cancellationToken = default)
        {
            var matching = LoadMatching(from, to, filters?.ToArray(), freeText: null, cancellationToken);

            long total = 0, errors = 0, warnings = 0, infos = 0, debugs = 0;
            foreach (var entry in matching)
            {
                total++;
                if (entry.Level == null) continue;
                if (entry.Level.Level >= LogSeverity.Error.Level)     errors++;
                else if (entry.Level.Level == LogSeverity.Warning.Level) warnings++;
                else if (entry.Level.Level == LogSeverity.Information.Level) infos++;
                else if (entry.Level.Level <= LogSeverity.Debug.Level) debugs++;
            }

            return Task.FromResult(new LogStatistics
            {
                From = from,
                To = to,
                TotalCount = total,
                ErrorCount = errors,
                WarningCount = warnings,
                InfoCount = infos,
                DebugCount = debugs
            });
        }

        public Task<LogStorageInfo> GetStorageInfoAsync(CancellationToken cancellationToken = default)
        {
            var breakdown = new Dictionary<string, long>();
            long? totalBytes = 0;

            if (Directory.Exists(m_logsPath))
            {
                foreach (var file in Directory.EnumerateFiles(m_logsPath, m_filePattern, SearchOption.TopDirectoryOnly))
                {
                    var info = new FileInfo(file);
                    breakdown[info.Name] = info.Length;
                    totalBytes += info.Length;
                }
            }
            else
            {
                totalBytes = null;
            }

            return Task.FromResult(new LogStorageInfo
            {
                UsedBytes = totalBytes,
                LimitBytes = null,
                TotalEntries = null,
                PeriodFrom = null,
                PeriodTo = null,
                Breakdown = breakdown.Count > 0 ? breakdown : null
            });
        }

        #endregion

        #region Tools

        private static (DateTime From, DateTime To) ResolveRange(LogQuery query)
        {
            var to = query.To ?? DateTime.UtcNow;
            DateTime from;
            if (query.From.HasValue)
                from = query.From.Value;
            else if (query.Lookback.HasValue)
                from = to - query.Lookback.Value;
            else
                from = to.AddHours(-1);
            return (from, to);
        }

        private List<LogEntry> LoadMatching(DateTime from, DateTime to, LogFilter[]? filters, string? freeText, CancellationToken ct)
        {
            var matched = new List<LogEntry>();
            int scanned = 0;
            int cap = m_options.MaxScannedEntries > 0 ? m_options.MaxScannedEntries : FileLogProviderOptions.DEFAULT_MAX_SCANNED_ENTRIES;

            if (!Directory.Exists(m_logsPath))
            {
                m_logger.LogWarning("FileLogQueryProvider: directory '{Path}' does not exist; returning empty result.", m_logsPath);
                return matched;
            }

            // Newest files first — capping by MaxScannedEntries should preferentially
            // keep recent entries rather than the oldest ones still on disk.
            var files = Directory.EnumerateFiles(m_logsPath, m_filePattern, SearchOption.TopDirectoryOnly)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTimeUtc);

            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();
                if (scanned >= cap)
                    break;

                ReadFile(file.FullName, from, to, filters, freeText, matched, cap, ref scanned, ct);
            }

            return matched;
        }

        private static void ReadFile(string path, DateTime from, DateTime to, LogFilter[]? filters, string? freeText,
                                     List<LogEntry> matched, int cap, ref int scanned, CancellationToken ct)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (scanned >= cap)
                        return;

                    ct.ThrowIfCancellationRequested();
                    scanned++;

                    var entry = LogEntryJsonParser.TryParse(line);
                    if (entry == null)
                        continue;

                    if (entry.Timestamp < from || entry.Timestamp > to)
                        continue;

                    if (!LogFilterMatcher.MatchesFreeText(entry, freeText))
                        continue;

                    if (!LogFilterMatcher.Matches(entry, filters))
                        continue;

                    matched.Add(entry);
                }
            }
            catch (IOException)
            {
                // Best-effort scan: skip a file we can't read this moment.
            }
        }

        private static string? Read(LogEntry entry, LogAttribute attribute)
        {
            if (LogAttribute.Level.Is(attribute.Value))         return entry.Level?.Value;
            if (LogAttribute.Message.Is(attribute.Value))       return entry.Message;
            if (LogAttribute.SourceContext.Is(attribute.Value)) return entry.SourceContext;
            if (LogAttribute.ServiceName.Is(attribute.Value))   return entry.ServiceName;
            if (LogAttribute.Host.Is(attribute.Value))          return entry.Host;
            if (LogAttribute.Environment.Is(attribute.Value))   return entry.Environment;
            if (LogAttribute.TraceId.Is(attribute.Value))       return entry.TraceId;
            if (LogAttribute.SpanId.Is(attribute.Value))        return entry.SpanId;
            return null;
        }

        #endregion
    }
}
