using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Plugins.Abstractions.Interfaces;

namespace OutWit.Shared.Storage.Providers
{
    /// <summary>
    /// Plugin interface for blob storage backends.
    /// Implementations: disk filesystem, Amazon S3, Azure Blob, etc.
    /// </summary>
    public interface IBlobStorageProviderPlugin : IWitPlugin
    {
        /// <summary>
        /// Registers the provider's <see cref="IBlobStorageProvider"/> implementation in the DI container.
        /// </summary>
        void Initialize(IServiceCollection services, string environment, Action? configure = null);

        /// <summary>
        /// Unique key identifying this provider (e.g., "Disk", "S3", "AzureBlob").
        /// Matched against Storage:ProviderKey in configuration.
        /// </summary>
        string Key { get; }
    }
}
