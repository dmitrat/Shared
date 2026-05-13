using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutWit.Common.Email;
using OutWit.Common.Plugins.Abstractions;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Shared.Email.Providers;

namespace OutWit.Shared.Email.Provider.Smtp
{
    /// <summary>
    /// SMTP email provider plugin. Reads SMTP configuration from the plugin's
    /// own <c>appsettings.json</c> (with environment-variable overrides) and
    /// registers a <see cref="SmtpEmailTransport"/> in the host DI container.
    /// Activate with <c>Email__ProviderKey=Smtp</c>.
    /// </summary>
    [WitPluginManifest("SMTP Email Provider", Version = "1.0.0")]
    public sealed class SmtpEmailProviderPlugin : WitPluginBase, IEmailProviderPlugin
    {
        #region Constants

        public const string KEY = "Smtp";

        #endregion

        #region IEmailProviderPlugin

        public string Key => KEY;

        #endregion

        #region IWitPlugin

        public override void Initialize(IServiceCollection services)
        {
            var options = ReadOptions();
            services.AddSingleton(options);
            services.AddSingleton<IEmailTransport>(sp =>
                new SmtpEmailTransport(
                    options,
                    sp.GetRequiredService<ILogger<SmtpEmailTransport>>()));
        }

        #endregion

        #region Tools

        private static SmtpOptions ReadOptions()
        {
            var pluginDir = Path.GetDirectoryName(typeof(SmtpEmailProviderPlugin).Assembly.Location)!;
            var config = new ConfigurationBuilder()
                .SetBasePath(pluginDir)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var options = config.GetSection("Smtp").Get<SmtpOptions>() ?? new SmtpOptions();

            // Defensive: env-var Smtp__Password should overwrite blank JSON value
            // via the standard binding above. Belt-and-braces in case the section
            // was empty in JSON.
            options.Password ??= config["Smtp:Password"];

            return options;
        }

        #endregion
    }
}
