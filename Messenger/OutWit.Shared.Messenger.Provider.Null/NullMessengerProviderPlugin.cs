using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutWit.Common.Messenger;
using OutWit.Common.Plugins.Abstractions;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Shared.Messenger.Providers;

namespace OutWit.Shared.Messenger.Provider.Null
{
    /// <summary>
    /// Fallback messenger provider — registers <see cref="NullMessengerTransport"/>
    /// in the host DI container. Mode is taken from the plugin's own
    /// <c>appsettings.json</c>:
    /// <code>
    /// { "Null": { "Mode": "LogOnly" } }
    /// </code>
    /// or via the environment variable <c>Null__Mode</c>. Default mode is
    /// <see cref="NullMessengerMode.LogOnly"/>.
    /// </summary>
    [WitPluginManifest("Null Messenger Provider", Version = "1.0.0")]
    public sealed class NullMessengerProviderPlugin : WitPluginBase, IMessengerProviderPlugin
    {
        #region Constants

        public const string KEY = "Null";

        #endregion

        #region IMessengerProviderPlugin

        public string Key => KEY;

        #endregion

        #region IWitPlugin

        public override void Initialize(IServiceCollection services)
        {
            var mode = ReadMode();

            services.AddSingleton<IMessengerTransport>(sp =>
                new NullMessengerTransport(
                    mode,
                    sp.GetRequiredService<ILogger<NullMessengerTransport>>()));
        }

        #endregion

        #region Tools

        private static NullMessengerMode ReadMode()
        {
            var pluginDir = Path.GetDirectoryName(typeof(NullMessengerProviderPlugin).Assembly.Location)!;
            var config = new ConfigurationBuilder()
                .SetBasePath(pluginDir)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var raw = config["Null:Mode"];
            if (string.IsNullOrWhiteSpace(raw))
                return NullMessengerMode.LogOnly;

            return Enum.TryParse<NullMessengerMode>(raw, ignoreCase: true, out var parsed)
                ? parsed
                : NullMessengerMode.LogOnly;
        }

        #endregion
    }
}
