namespace OutWit.Shared.Secrets.Provider.File
{
    /// <summary>
    /// Options for <see cref="SecretStoreFile"/>. The provider is never selected
    /// automatically — configuration names it, and these options say where and how.
    /// </summary>
    public sealed class SecretStoreFileOptions
    {
        #region Properties

        /// <summary>
        /// Directory the secret files live in. Required. Created with restrictive
        /// permissions if absent.
        /// </summary>
        public string DirectoryPath { get; init; } = "";

        /// <summary>
        /// Protect payloads with the platform key where one exists — Windows DPAPI machine
        /// scope, reported as <see cref="Providers.SecretProtection.FileWithPlatformKey"/>.
        /// On platforms without one the store is honestly
        /// <see cref="Providers.SecretProtection.FileOnly"/> whatever this says.
        /// Default true.
        /// </summary>
        public bool UsePlatformKey { get; init; } = true;

        #endregion
    }
}
