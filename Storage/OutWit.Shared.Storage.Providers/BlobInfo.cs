using System;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Shared.Storage.Providers
{
    /// <summary>
    /// Metadata for a stored blob.
    /// </summary>
    [MemoryPackable]
    public partial class BlobInfo : ModelBase
    {
        #region Properties

        public Guid Id { get; set; }

        public string FileName { get; set; } = null!;

        public long Size { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? ExpiresAtUtc { get; set; }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not BlobInfo other)
                return false;

            return Id.Is(other.Id)
                   && FileName.Is(other.FileName)
                   && Size.Is(other.Size);
        }

        public override ModelBase Clone()
        {
            return new BlobInfo
            {
                Id = Id,
                FileName = FileName,
                Size = Size,
                CreatedAtUtc = CreatedAtUtc,
                ExpiresAtUtc = ExpiresAtUtc
            };
        }

        #endregion
    }
}
