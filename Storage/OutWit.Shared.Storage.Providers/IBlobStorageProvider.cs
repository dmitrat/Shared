using System;
using System.IO;
using System.Threading.Tasks;

namespace OutWit.Shared.Storage.Providers
{
    /// <summary>
    /// Low-level blob I/O provider abstraction.
    /// Contains only storage operations — no business logic, sessions, or metadata management.
    /// Implementations are swappable via plugin system (disk, S3, Azure Blob, etc.).
    /// </summary>
    public interface IBlobStorageProvider
    {
        /// <summary>
        /// Writes blob data from a stream. Creates or overwrites the file.
        /// </summary>
        Task WriteAsync(Guid blobId, string fileName, Stream data);

        /// <summary>
        /// Appends a chunk to an existing blob file. Creates the file if it does not exist.
        /// </summary>
        Task AppendAsync(Guid blobId, string fileName, byte[] chunk);

        /// <summary>
        /// Opens a read stream for the specified blob file.
        /// Caller is responsible for disposal.
        /// </summary>
        Task<Stream> ReadAsync(Guid blobId, string fileName);

        /// <summary>
        /// Reads a specific byte range from a blob file.
        /// </summary>
        Task<byte[]> ReadChunkAsync(Guid blobId, string fileName, long offset, int length);

        /// <summary>
        /// Deletes a blob and all associated files.
        /// </summary>
        Task DeleteAsync(Guid blobId);

        /// <summary>
        /// Checks whether a specific file exists within a blob container.
        /// </summary>
        Task<bool> ExistsAsync(Guid blobId, string fileName);

        /// <summary>
        /// Returns the size in bytes of a specific file within a blob container.
        /// </summary>
        Task<long> GetSizeAsync(Guid blobId, string fileName);
    }
}
