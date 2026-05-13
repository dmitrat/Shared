using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutWit.Common.Email;
using OutWit.Common.Plugins.Abstractions;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Shared.Email.Providers;

namespace OutWit.Shared.Email.Provider.Null
{
    /// <summary>
    /// Fallback email provider — registers <see cref="NullEmailTransport"/> in the
    /// host DI container. Mode is taken from the plugin's own <c>appsettings.json</c>:
    /// <code>
    /// { "Null": { "Mode": "LogOnly" } }
    /// </code>
    /// or via the environment variable <c>Null__Mode</c>.
    /// Default mode is <see cref="NullEmailMode.LogOnly"/> — operators get a working
    /// system out of the box and only flip to <c>Drop</c> when they explicitly want
    /// "no outbound email" behaviour.
    /// </summary>
    [WitPluginManifest("Null Email Provider", Version = "1.0.0")]
    public sealed class NullEmailProviderPlugin : WitPluginBase, IEmailProviderPlugin
    {
        #region Constants

        public const string KEY = "Null";

        #endregion

        #region IEmailProviderPlugin

        public string Key => KEY;

        #endregion

        #region IWitPlugin

        public override void Initialize(IServiceCollection services)
        {
            var mode = ReadMode();

            services.AddSingleton<IEmailTransport>(sp =>
                new NullEmailTransport(
                    mode,
                    sp.GetRequiredService<ILogger<NullEmailTransport>>()));
        }

        #endregion

        #region Tools

        private static NullEmailMode ReadMode()
        {
            var pluginDir = Path.GetDirectoryName(typeof(NullEmailProviderPlugin).Assembly.Location)!;
            var config = new ConfigurationBuilder()
                .SetBasePath(pluginDir)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var raw = config["Null:Mode"];
            if (string.IsNullOrWhiteSpace(raw))
                return NullEmailMode.LogOnly;

            return Enum.TryParse<NullEmailMode>(raw, ignoreCase: true, out var parsed)
                ? parsed
                : NullEmailMode.LogOnly;
        }

        #endregion
    }
}
