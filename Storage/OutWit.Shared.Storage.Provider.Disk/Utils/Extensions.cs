using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Configuration;
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
            var configuration = ConfigurationUtils
                .For(typeof(DiskBlobStorageProviderPlugin).Assembly)
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
