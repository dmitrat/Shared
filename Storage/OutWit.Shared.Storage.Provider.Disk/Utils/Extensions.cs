using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Configuration;
using OutWit.Common.Plugins;
using OutWit.Shared.Storage.Providers;

namespace OutWit.Shared.Storage.Provider.Disk.Utils
{
    /// <summary>
    /// Extension methods for registering the disk blob storage provider.
    /// </summary>
    public static class Extensions
    {
        #region Constants

        /// <summary>
        /// Configuration section of the provider (<c>DiskBlobStorage</c>).
        /// </summary>
        public const string SECTION_NAME = "DiskBlobStorage";

        /// <summary>
        /// Key of the storage root inside <see cref="SECTION_NAME"/>; as an environment
        /// variable: <c>DiskBlobStorage__StoragePath</c>.
        /// </summary>
        public const string STORAGE_PATH_KEY = "StoragePath";

        #endregion

        #region Functions

        /// <summary>
        /// Registers the disk blob storage provider and its settings.
        /// </summary>
        public static IServiceCollection AddDiskBlobStorage(this IServiceCollection me, string environment)
        {
            var settings = ResolveSettings(environment);

            me.AddSingleton(settings);
            me.AddSingleton<IBlobStorageProvider, DiskBlobStorageProvider>();

            return me;
        }

        /// <summary>
        /// Resolves the provider settings: the plugin's own <c>appsettings.json</c> (and the
        /// environment-specific file layered on it) read once, then the process environment
        /// variables on top - <c>DiskBlobStorage__StoragePath</c> wins, so a container deploy
        /// points the store at a mounted volume without editing the staged module folder.
        /// A relative path roots under <see cref="AppContext.BaseDirectory"/>.
        /// </summary>
        /// <param name="environment">Configuration environment name (for example Development, Production).</param>
        /// <returns>The settings with an absolute <see cref="DiskBlobStorageSettings.StoragePath"/>.</returns>
        public static DiskBlobStorageSettings ResolveSettings(string environment)
        {
            // Route config lookup through the loader-registered home directory
            // (WitPluginHostContexts). Under shared-context loading
            // typeof(...).Assembly.Location points at the default-ALC PR-graph
            // copy, not the staged module folder - so the legacy For(Assembly)
            // overload reads appsettings.json from the host bin, not from
            // @Storage/disk.module/. For(IAssemblyContext) uses the path the
            // loader actually scanned. The files are read once, without a
            // watcher: the settings never change for the process lifetime.
            var pluginContext = WitPluginHostContexts.For(typeof(DiskBlobStorageProviderPlugin).Assembly);
            var configuration = new ConfigurationBuilder()
                .AddConfiguration(ConfigurationUtils
                    .For(pluginContext)
                    .WithEnvironment(environment)
                    .WithReloadOnChange(false)
                    .Build())
                .AddEnvironmentVariables()
                .Build();

            var settings = new DiskBlobStorageSettings();
            var storagePath = configuration.GetSection(SECTION_NAME)?[STORAGE_PATH_KEY];
            if (!string.IsNullOrWhiteSpace(storagePath))
                settings.StoragePath = storagePath;

            if (!Path.IsPathRooted(settings.StoragePath))
                settings.StoragePath = Path.Combine(AppContext.BaseDirectory, settings.StoragePath);

            return settings;
        }

        #endregion
    }
}
