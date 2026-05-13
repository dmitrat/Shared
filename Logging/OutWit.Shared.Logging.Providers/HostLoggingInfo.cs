namespace OutWit.Shared.Logging.Providers
{
    /// <summary>
    /// Information the host shares with file-scanning log provider plugins — the
    /// directory + filename pattern its log sink writes to. Lets the File provider
    /// avoid duplicating Serilog configuration: the host registers a singleton of
    /// this type before invoking <c>plugin.Initialize(services)</c>, and the plugin
    /// reads it at first query.
    /// </summary>
    /// <remarks>
    /// Vendor plugins (NewRelic, Loki, …) ignore this object — they read their own
    /// API tokens / endpoints from their own appsettings. Only file-scanning plugins
    /// (currently <c>OutWit.Shared.Logging.Provider.File</c>) consume it.
    /// </remarks>
    public sealed class HostLoggingInfo
    {
        #region Properties

        /// <summary>
        /// Absolute path to the directory the host's log sink writes into
        /// (e.g. <c>/app/logs</c>). When <c>null</c>, the File plugin falls back
        /// to its own <c>File:LogsPath</c> setting.
        /// </summary>
        public string? LogsPath { get; init; }

        /// <summary>
        /// Glob pattern matching the host's log files inside <see cref="LogsPath"/>
        /// (e.g. <c>log-*.json</c>). When <c>null</c>, the File plugin uses its own
        /// <c>File:Pattern</c> setting.
        /// </summary>
        public string? FilePattern { get; init; }

        #endregion
    }
}
