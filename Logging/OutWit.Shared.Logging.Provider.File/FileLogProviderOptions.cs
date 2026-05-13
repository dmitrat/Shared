namespace OutWit.Shared.Logging.Provider.File
{
    /// <summary>
    /// Configuration for the file-scanning log provider. Bound from the plugin's
    /// own <c>appsettings.json</c> (with env-var overrides via standard .NET
    /// configuration binding). The host's log directory and filename pattern come
    /// from <see cref="OutWit.Shared.Logging.Providers.HostLoggingInfo"/> instead
    /// of duplicating them here — the server already knows where it writes logs.
    /// </summary>
    public sealed class FileLogProviderOptions
    {
        #region Constants

        public const int DEFAULT_MAX_SCANNED_ENTRIES = 50_000;

        public const string FALLBACK_LOGS_PATH = "logs";

        public const string FALLBACK_FILE_PATTERN = "log-*.json";

        #endregion

        #region Properties

        /// <summary>
        /// Maximum number of log lines parsed per query. Caps the work each
        /// request can do regardless of how much history sits on disk. Default 50000.
        /// </summary>
        public int MaxScannedEntries { get; set; } = DEFAULT_MAX_SCANNED_ENTRIES;

        /// <summary>
        /// Fallback directory used only when <see cref="OutWit.Shared.Logging.Providers.HostLoggingInfo.LogsPath"/>
        /// was not registered by the host. In a normal WitIdentity deployment this stays
        /// at its default — the host always supplies the real path.
        /// </summary>
        public string FallbackLogsPath { get; set; } = FALLBACK_LOGS_PATH;

        /// <summary>
        /// Fallback glob pattern used only when <see cref="OutWit.Shared.Logging.Providers.HostLoggingInfo.FilePattern"/>
        /// was not registered by the host.
        /// </summary>
        public string FallbackFilePattern { get; set; } = FALLBACK_FILE_PATTERN;

        #endregion
    }
}
