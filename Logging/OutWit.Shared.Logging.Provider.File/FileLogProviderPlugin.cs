using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutWit.Common.Logging.Query;
using OutWit.Common.Plugins.Abstractions;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Shared.Logging.Providers;

namespace OutWit.Shared.Logging.Provider.File
{
    /// <summary>
    /// File-scanning log provider plugin — the OutWit "no external account needed"
    /// fallback. Reads the host's Serilog NDJSON files in-place and turns them into
    /// query results through a neutral <see cref="ILogQueryProvider"/>.
    /// </summary>
    /// <remarks>
    /// The host's log directory + filename pattern come from
    /// <see cref="HostLoggingInfo"/> when the host registered one before
    /// <see cref="Initialize"/>. Otherwise the plugin falls back to its own
    /// <c>File:FallbackLogsPath</c> / <c>File:FallbackFilePattern</c>.
    /// Activate with <c>Logging__ProviderKey=File</c>.
    /// </remarks>
    [WitPluginManifest("File Log Provider", Version = "1.0.0")]
    public sealed class FileLogProviderPlugin : WitPluginBase, ILogProviderPlugin
    {
        #region Constants

        public const string KEY = "File";

        #endregion

        #region ILogProviderPlugin

        public string Key => KEY;

        #endregion

        #region IWitPlugin

        public override void Initialize(IServiceCollection services)
        {
            var options = ReadOptions();
            services.AddSingleton(options);

            // Resolve host-supplied path lazily at first query so the host can
            // register HostLoggingInfo any time before BuildServiceProvider.
            services.AddSingleton<ILogQueryProvider>(sp =>
            {
                var host = sp.GetService<HostLoggingInfo>();
                var path = !string.IsNullOrWhiteSpace(host?.LogsPath)
                    ? host!.LogsPath!
                    : ResolveLocalPath(options.FallbackLogsPath);
                var pattern = !string.IsNullOrWhiteSpace(host?.FilePattern)
                    ? host!.FilePattern!
                    : options.FallbackFilePattern;

                return new FileLogQueryProvider(
                    path,
                    pattern,
                    options,
                    sp.GetRequiredService<ILogger<FileLogQueryProvider>>());
            });
        }

        #endregion

        #region Tools

        private static FileLogProviderOptions ReadOptions()
        {
            var pluginDir = Path.GetDirectoryName(typeof(FileLogProviderPlugin).Assembly.Location)!;
            var config = new ConfigurationBuilder()
                .SetBasePath(pluginDir)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var options = config.GetSection("File").Get<FileLogProviderOptions>() ?? new FileLogProviderOptions();
            if (options.MaxScannedEntries <= 0)
                options.MaxScannedEntries = FileLogProviderOptions.DEFAULT_MAX_SCANNED_ENTRIES;
            if (string.IsNullOrWhiteSpace(options.FallbackLogsPath))
                options.FallbackLogsPath = FileLogProviderOptions.FALLBACK_LOGS_PATH;
            if (string.IsNullOrWhiteSpace(options.FallbackFilePattern))
                options.FallbackFilePattern = FileLogProviderOptions.FALLBACK_FILE_PATTERN;
            return options;
        }

        private static string ResolveLocalPath(string fallback)
        {
            return Path.IsPathRooted(fallback)
                ? fallback
                : Path.Combine(AppContext.BaseDirectory, fallback);
        }

        #endregion
    }
}
