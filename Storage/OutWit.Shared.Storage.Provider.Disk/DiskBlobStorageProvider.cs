using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Shared.Storage.Providers;

namespace OutWit.Shared.Storage.Provider.Disk
{
    /// <summary>
    /// Blob storage provider implementation using local filesystem.
    /// Each blob is stored as a directory containing its files.
    /// </summary>
    public sealed class DiskBlobStorageProvider : IBlobStorageProvider
    {
        #region Fields

        private readonly string m_basePath;
        private readonly ILogger<DiskBlobStorageProvider> m_logger;

        #endregion

        #region Constructors

        public DiskBlobStorageProvider(DiskBlobStorageSettings settings, ILogger<DiskBlobStorageProvider> logger)
        {
            m_basePath = settings.StoragePath;
            m_logger = logger;

            if (!Directory.Exists(m_basePath))
                Directory.CreateDirectory(m_basePath);
        }

        #endregion

        #region IBlobStorageProvider

        /// <inheritdoc/>
        public async Task WriteAsync(Guid blobId, string fileName, Stream data)
        {
            ValidateFileName(fileName);
            var dir = GetBlobDirectory(blobId);
            Directory.CreateDirectory(dir);

            var path = Path.Combine(dir, fileName);
            await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            await data.CopyToAsync(fileStream);

            m_logger.LogDebug("Blob {BlobId}/{FileName} written ({Size} bytes)",
                blobId, fileName, fileStream.Length);
        }

        /// <inheritdoc/>
        public async Task AppendAsync(Guid blobId, string fileName, byte[] chunk)
        {
            ValidateFileName(fileName);
            var dir = GetBlobDirectory(blobId);
            Directory.CreateDirectory(dir);

            var path = Path.Combine(dir, fileName);
            await using var fileStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None);
            await fileStream.WriteAsync(chunk);
        }

        /// <inheritdoc/>
        public Task<Stream> ReadAsync(Guid blobId, string fileName)
        {
            var path = GetFilePath(blobId, fileName);
            EnsureFileExists(path, blobId, fileName);

            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult(stream);
        }

        /// <inheritdoc/>
        public async Task<byte[]> ReadChunkAsync(Guid blobId, string fileName, long offset, int length)
        {
            var path = GetFilePath(blobId, fileName);
            EnsureFileExists(path, blobId, fileName);

            await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            fileStream.Seek(offset, SeekOrigin.Begin);

            var buffer = new byte[length];
            int bytesRead = await fileStream.ReadAsync(buffer, 0, length);

            return bytesRead == length ? buffer : buffer[..bytesRead];
        }

        /// <inheritdoc/>
        public Task DeleteAsync(Guid blobId)
        {
            var dir = GetBlobDirectory(blobId);

            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
                m_logger.LogDebug("Blob {BlobId} deleted", blobId);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> ExistsAsync(Guid blobId, string fileName)
        {
            var path = GetFilePath(blobId, fileName);
            return Task.FromResult(File.Exists(path));
        }

        /// <inheritdoc/>
        public Task<long> GetSizeAsync(Guid blobId, string fileName)
        {
            var path = GetFilePath(blobId, fileName);
            EnsureFileExists(path, blobId, fileName);

            return Task.FromResult(new FileInfo(path).Length);
        }

        #endregion

        #region Tools

        private string GetBlobDirectory(Guid blobId)
        {
            return Path.Combine(m_basePath, blobId.ToString("N"));
        }

        private string GetFilePath(Guid blobId, string fileName)
        {
            ValidateFileName(fileName);
            return Path.Combine(GetBlobDirectory(blobId), fileName);
        }

        private static void ValidateFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty.", nameof(fileName));

            if (fileName.Contains("..") ||
                fileName.Contains('/') ||
                fileName.Contains('\\') ||
                Path.IsPathRooted(fileName))
                throw new ArgumentException(
                    $"Invalid file name: '{fileName}'. Path separators and traversal sequences are not allowed.",
                    nameof(fileName));

            var invalidChars = Path.GetInvalidFileNameChars();
            if (fileName.IndexOfAny(invalidChars) >= 0)
                throw new ArgumentException(
                    $"File name contains invalid characters: '{fileName}'.",
                    nameof(fileName));
        }

        private static void EnsureFileExists(string path, Guid blobId, string fileName)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    $"Blob file not found: {blobId}/{fileName}", path);
        }

        #endregion
    }
}
