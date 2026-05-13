using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Logging.Loki;
using OutWit.Common.Logging.Query;
using OutWit.Common.Plugins.Abstractions;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Shared.Logging.Providers;

namespace OutWit.Shared.Logging.Provider.Loki
{
    /// <summary>
    /// Grafana Loki log provider plugin. Thin wrapper over
    /// <see cref="OutWit.Common.Logging.Loki.LokiLogQueryProvider"/> that
    /// registers an <see cref="ILogQueryProvider"/> talking to a Loki
    /// <c>/loki/api/v1/*</c> endpoint. Activate via <c>Logging__ProviderKey=Loki</c>.
    /// </summary>
    [WitPluginManifest("Loki Log Provider", Version = "1.0.0")]
    public sealed class LokiLogProviderPlugin : WitPluginBase, ILogProviderPlugin
    {
        #region Constants

        public const string KEY = "Loki";

        #endregion

        #region ILogProviderPlugin

        public string Key => KEY;

        #endregion

        #region IWitPlugin

        public override void Initialize(IServiceCollection services)
        {
            var options = ReadOptions();
            services.AddSingleton(options);

            // Typed HttpClient pattern — DI builds LokiHttpClient with HttpClient
            // from the factory + LokiOptions from the singleton.
            services.AddHttpClient<LokiHttpClient>();

            services.AddSingleton<ILogQueryProvider, LokiLogQueryProvider>();
        }

        #endregion

        #region Tools

        private static LokiOptions ReadOptions()
        {
            var pluginDir = Path.GetDirectoryName(typeof(LokiLogProviderPlugin).Assembly.Location)!;
            var config = new ConfigurationBuilder()
                .SetBasePath(pluginDir)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var options = config.GetSection("Loki").Get<LokiOptions>() ?? new LokiOptions();

            // Belt-and-braces: env vars Loki__Password / Loki__Username override
            // any blank JSON values via standard binding above.
            if (string.IsNullOrWhiteSpace(options.Password))
                options.Password = config["Loki:Password"];

            if (string.IsNullOrWhiteSpace(options.BaseUrl))
                throw new InvalidOperationException(
                    "Loki:BaseUrl is not configured. Set Logging__ProviderKey=Loki only after supplying Loki__BaseUrl.");

            return options;
        }

        #endregion
    }
}
