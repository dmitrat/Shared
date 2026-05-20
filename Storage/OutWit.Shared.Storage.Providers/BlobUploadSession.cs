using System;

namespace OutWit.Shared.Storage.Providers
{
    /// <summary>
    /// Tracks the state of an in-progress chunked blob upload.
    /// </summary>
    public sealed class BlobUploadSession
    {
        #region Properties

        public Guid UploadId { get; set; }

        public string FileName { get; set; } = null!;

        public long TotalSize { get; set; }

        public long BytesReceived { get; set; }

        public int NextChunkIndex { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        #endregion
    }
}
