namespace OutWit.Shared.Storage.Providers
{
    /// <summary>
    /// Configuration settings for the blob storage subsystem.
    /// </summary>
    public interface IBlobStorageSettings
    {
        /// <summary>
        /// Provider key matching a loaded <see cref="IBlobStorageProviderPlugin.Key"/>.
        /// </summary>
        string ProviderKey { get; }

        /// <summary>
        /// Path to the directory containing storage provider plugin modules.
        /// </summary>
        string PluginsPath { get; }

        /// <summary>
        /// Maximum allowed blob size in bytes.
        /// </summary>
        long MaxBlobSize { get; }

        /// <summary>
        /// Chunk size for chunked uploads in bytes.
        /// </summary>
        int ChunkSize { get; }

        /// <summary>
        /// Size threshold above which transfers should prefer chunked mode instead of one-shot byte-array paths.
        /// </summary>
        long ChunkedTransferThresholdBytes { get; }

        /// <summary>
        /// Default time-to-live for blobs in minutes.
        /// </summary>
        int DefaultTtlMinutes { get; }

        /// <summary>
        /// Interval between cleanup sweeps in minutes.
        /// </summary>
        int CleanupIntervalMinutes { get; }

        /// <summary>
        /// Timeout for incomplete upload sessions in minutes.
        /// </summary>
        int UploadSessionTimeoutMinutes { get; }
    }
}
