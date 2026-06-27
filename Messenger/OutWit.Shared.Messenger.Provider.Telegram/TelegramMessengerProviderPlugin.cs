using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Messenger;
using OutWit.Common.Plugins.Abstractions;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Shared.Messenger.Providers;

namespace OutWit.Shared.Messenger.Provider.Telegram
{
    /// <summary>
    /// Telegram messenger provider plugin. Reads its configuration from the plugin's
    /// own <c>appsettings.json</c> (with env-var overrides — typically
    /// <c>Telegram__BotToken</c>) and registers a <see cref="TelegramMessengerTransport"/>
    /// in the host DI container. Activate with <c>Messenger__ProviderKey=Telegram</c>.
    /// </summary>
    [WitPluginManifest("Telegram Messenger Provider", Version = "1.0.0")]
    public sealed class TelegramMessengerProviderPlugin : WitPluginBase, IMessengerProviderPlugin
    {
        #region Constants

        public const string KEY = "Telegram";

        #endregion

        #region IMessengerProviderPlugin

        public string Key => KEY;

        #endregion

        #region IWitPlugin

        public override void Initialize(IServiceCollection services)
        {
            var options = ReadOptions();

            services.AddSingleton(options);
            services.AddHttpClient<TelegramMessengerTransport>();
            services.AddSingleton<IMessengerTransport>(sp =>
                sp.GetRequiredService<TelegramMessengerTransport>());
        }

        #endregion

        #region Tools

        private static TelegramOptions ReadOptions()
        {
            var pluginDir = Path.GetDirectoryName(typeof(TelegramMessengerProviderPlugin).Assembly.Location)!;
            var config = new ConfigurationBuilder()
                .SetBasePath(pluginDir)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var options = config.GetSection("Telegram").Get<TelegramOptions>() ?? new TelegramOptions();

            // Belt-and-braces: env-var Telegram__BotToken should win via standard binding,
            // but read it directly in case the JSON section was empty.
            if (string.IsNullOrWhiteSpace(options.BotToken))
                options.BotToken = config["Telegram:BotToken"];

            return options;
        }

        #endregion
    }
}
