using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Logging.NewRelic;
using OutWit.Common.Logging.NewRelic.Interfaces;
using OutWit.Common.Logging.NewRelic.Model;
using OutWit.Common.Logging.Query;
using OutWit.Common.Plugins.Abstractions;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Shared.Logging.Providers;

namespace OutWit.Shared.Logging.Provider.NewRelic
{
    /// <summary>
    /// NewRelic NerdGraph log provider plugin. Thin wrapper over
    /// <see cref="OutWit.Common.Logging.NewRelic.NewRelicProvider"/> that
    /// registers an <see cref="ILogQueryProvider"/> (and the NR-specific
    /// <see cref="INewRelicProvider"/> superset, for consumers that need the
    /// billing-style <c>GetDataConsumptionAsync</c>) when activated via
    /// <c>Logging__ProviderKey=NewRelic</c>.
    /// </summary>
    [WitPluginManifest("NewRelic Log Provider", Version = "1.0.0")]
    public sealed class NewRelicLogProviderPlugin : WitPluginBase, ILogProviderPlugin
    {
        #region Constants

        public const string KEY = "NewRelic";

        #endregion

        #region ILogProviderPlugin

        public string Key => KEY;

        #endregion

        #region IWitPlugin

        public override void Initialize(IServiceCollection services)
        {
            var options = ReadOptions();
            services.AddSingleton(options);

            // Typed HttpClient pattern — DI builds NewRelicHttpClient with HttpClient
            // from the factory + NewRelicClientOptions from the singleton.
            services.AddHttpClient<NewRelicHttpClient>();

            services.AddSingleton<INewRelicProvider, NewRelicProvider>();
            services.AddSingleton<ILogQueryProvider>(sp => sp.GetRequiredService<INewRelicProvider>());
        }

        #endregion

        #region Tools

        private static NewRelicClientOptions ReadOptions()
        {
            var pluginDir = Path.GetDirectoryName(typeof(NewRelicLogProviderPlugin).Assembly.Location)!;
            var config = new ConfigurationBuilder()
                .SetBasePath(pluginDir)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var options = config.GetSection("NewRelic").Get<NewRelicClientOptions>()
                          ?? new NewRelicClientOptions { ApiKey = string.Empty, AccountId = 0 };

            if (string.IsNullOrWhiteSpace(options.ApiKey))
                options.ApiKey = config["NewRelic:ApiKey"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(options.ApiKey))
                throw new InvalidOperationException(
                    "NewRelic:ApiKey is not configured. Set Logging__ProviderKey=NewRelic only after supplying NewRelic__ApiKey.");

            if (options.AccountId == 0)
                throw new InvalidOperationException(
                    "NewRelic:AccountId is not configured. Supply NewRelic__AccountId.");

            return options;
        }

        #endregion
    }
}
