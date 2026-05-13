using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutWit.Common.Email;
using OutWit.Common.Plugins.Abstractions;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Shared.Email.Providers;
using Resend;

namespace OutWit.Shared.Email.Provider.Resend
{
    /// <summary>
    /// Resend email provider plugin. Reads Resend configuration from the
    /// plugin's own <c>appsettings.json</c> (with environment-variable overrides
    /// — typically <c>Resend__ApiToken</c>) and registers a
    /// <see cref="ResendEmailTransport"/> in the host DI container.
    /// Activate with <c>Email__ProviderKey=Resend</c>.
    /// </summary>
    [WitPluginManifest("Resend Email Provider", Version = "1.0.0")]
    public sealed class ResendEmailProviderPlugin : WitPluginBase, IEmailProviderPlugin
    {
        #region Constants

        public const string KEY = "Resend";

        #endregion

        #region IEmailProviderPlugin

        public string Key => KEY;

        #endregion

        #region IWitPlugin

        public override void Initialize(IServiceCollection services)
        {
            var options = ReadOptions();

            services.AddSingleton(options);

            // Resend SDK plumbing: typed HttpClient + bound ResendClientOptions.
            services.AddHttpClient<ResendClient>();
            services.Configure<ResendClientOptions>(o =>
            {
                o.ApiToken = options.ApiToken ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(options.ApiUrl))
                    o.ApiUrl = options.ApiUrl;
            });
            services.AddTransient<IResend, ResendClient>();

            services.AddSingleton<IEmailTransport>(sp =>
                new ResendEmailTransport(
                    sp.GetRequiredService<IResend>(),
                    sp.GetRequiredService<ILogger<ResendEmailTransport>>()));
        }

        #endregion

        #region Tools

        private static ResendOptions ReadOptions()
        {
            var pluginDir = Path.GetDirectoryName(typeof(ResendEmailProviderPlugin).Assembly.Location)!;
            var config = new ConfigurationBuilder()
                .SetBasePath(pluginDir)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var options = config.GetSection("Resend").Get<ResendOptions>() ?? new ResendOptions();

            // Belt-and-braces: env-var Resend__ApiToken should win via standard binding,
            // but read it directly in case the JSON section was empty.
            if (string.IsNullOrWhiteSpace(options.ApiToken))
                options.ApiToken = config["Resend:ApiToken"];

            return options;
        }

        #endregion
    }
}
