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
        /// <summary>
        /// Registers the disk blob storage provider and its settings.
        /// </summary>
        public static IServiceCollection AddDiskBlobStorage(this IServiceCollection me, string environment)
        {
            // Route config lookup through the loader-registered home directory
            // (WitPluginHostContexts). Under shared-context loading
            // typeof(...).Assembly.Location points at the default-ALC PR-graph
            // copy, not the staged module folder — so the legacy For(Assembly)
            // overload reads appsettings.json from the host bin, not from
            // @Storage/disk.module/. For(IAssemblyContext) uses the path the
            // loader actually scanned.
            var pluginContext = WitPluginHostContexts.For(typeof(DiskBlobStorageProviderPlugin).Assembly);
            var configuration = ConfigurationUtils
                .For(pluginContext)
                .WithEnvironment(environment)
                .Build();

            var settings = new DiskBlobStorageSettings();
            var storagePath = configuration.GetSection("DiskBlobStorage")?["StoragePath"];
            if (!string.IsNullOrEmpty(storagePath))
                settings.StoragePath = storagePath;

            if (!Path.IsPathRooted(settings.StoragePath))
                settings.StoragePath = Path.Combine(AppContext.BaseDirectory, settings.StoragePath);

            me.AddSingleton(settings);
            me.AddSingleton<IBlobStorageProvider, DiskBlobStorageProvider>();

            return me;
        }
    }
}
