using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Plugins.Abstractions;
using OutWit.Common.Plugins.Abstractions.Attributes;
using OutWit.Shared.Storage.Provider.Disk.Utils;
using OutWit.Shared.Storage.Providers;

namespace OutWit.Shared.Storage.Provider.Disk
{
    /// <summary>
    /// Blob storage provider plugin for local filesystem storage.
    /// Stores blobs as files in a configurable directory.
    /// </summary>
    [WitPluginManifest("Disk Blob Storage", Version = "1.0.0")]
    public sealed class DiskBlobStorageProviderPlugin : WitPluginBase, IBlobStorageProviderPlugin
    {
        #region Constants

        private const string PROVIDER_KEY = "Disk";

        #endregion

        #region IBlobStorageProviderPlugin

        public void Initialize(IServiceCollection services, string environment, Action? configure = null)
        {
            services.AddDiskBlobStorage(environment);
        }

        public string Key => PROVIDER_KEY;

        #endregion
    }
}
