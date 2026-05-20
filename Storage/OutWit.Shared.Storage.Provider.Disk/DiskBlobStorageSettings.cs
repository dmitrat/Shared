namespace OutWit.Shared.Storage.Provider.Disk
{
    /// <summary>
    /// Configuration settings specific to the disk blob storage provider.
    /// </summary>
    public sealed class DiskBlobStorageSettings
    {
        #region Properties

        /// <summary>
        /// Root directory path for blob file storage.
        /// Relative paths resolved from <see cref="System.AppContext.BaseDirectory"/>.
        /// </summary>
        public string StoragePath { get; set; } = "@Blobs";

        #endregion
    }
}
