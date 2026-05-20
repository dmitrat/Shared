using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Plugins;

namespace OutWit.Shared.Storage.Providers
{
    /// <summary>
    /// Extension methods for registering blob storage in the DI container.
    /// Loads the appropriate provider plugin (Disk / S3 / Azure / …) based on configuration.
    /// </summary>
    public static class StorageUtils
    {
        private const string DEFAULT_PLUGINS_FOLDER = "@Storage";

        /// <summary>
        /// Registers blob storage services using the configured provider.
        /// </summary>
        public static IServiceCollection AddBlobStorage(this IServiceCollection me, IBlobStorageSettings settings, string environment, Action? configure = null)
        {
            var providerKey = settings.ProviderKey;
            var pluginsPath = settings.PluginsPath ?? DEFAULT_PLUGINS_FOLDER;

            if (string.IsNullOrEmpty(providerKey))
                throw new InvalidOperationException("Storage:ProviderKey is not specified in configuration.");

            var fullPluginsPath = Path.IsPathRooted(pluginsPath)
                ? pluginsPath
                : Path.Combine(AppContext.BaseDirectory, pluginsPath);

            if (!Directory.Exists(fullPluginsPath))
                Directory.CreateDirectory(fullPluginsPath);

            var pluginLoader = new WitPluginLoader<IBlobStorageProviderPlugin>(fullPluginsPath, false, null);
            me.AddSingleton(pluginLoader);

            pluginLoader.Load();

            var plugin = pluginLoader.FirstOrDefault(p => p.Key.Equals(providerKey, StringComparison.OrdinalIgnoreCase));
            if (plugin == null)
                throw new InvalidOperationException($"Blob storage plugin with key '{providerKey}' not found in '{fullPluginsPath}'.");

            plugin.Initialize(me, environment, configure);

            me.AddSingleton(settings);

            return me;
        }
    }
}
