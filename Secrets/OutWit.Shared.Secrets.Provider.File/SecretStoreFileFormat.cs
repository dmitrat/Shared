using System;

namespace OutWit.Shared.Secrets.Provider.File
{
    /// <summary>
    /// The on-disk envelope: "WSEC" magic, a format version, a protection marker, and the
    /// payload. Binary rather than JSON deliberately — the payload is a secret or its
    /// ciphertext and has no business being readable, and no serializer keeps the provider
    /// NativeAOT-clean.
    /// </summary>
    internal static class SecretStoreFileFormat
    {
        #region Constants

        internal const byte FORMAT_VERSION = 1;

        internal const byte PROTECTION_NONE = 0;
        internal const byte PROTECTION_DPAPI_MACHINE = 1;

        private const int HEADER_LENGTH = 8;

        private static readonly byte[] MAGIC = { (byte)'W', (byte)'S', (byte)'E', (byte)'C' };

        #endregion

        #region Functions

        /// <summary>
        /// Wraps a payload into an envelope.
        /// </summary>
        /// <param name="protection">The protection marker byte.</param>
        /// <param name="payload">The (possibly protected) payload.</param>
        /// <returns>The envelope bytes.</returns>
        internal static byte[] Build(byte protection, ReadOnlySpan<byte> payload)
        {
            byte[] envelope = new byte[HEADER_LENGTH + payload.Length];

            MAGIC.CopyTo(envelope, 0);
            envelope[4] = FORMAT_VERSION;
            envelope[5] = protection;
            envelope[6] = 0;
            envelope[7] = 0;
            payload.CopyTo(envelope.AsSpan(HEADER_LENGTH));

            return envelope;
        }

        /// <summary>
        /// Unwraps an envelope.
        /// </summary>
        /// <param name="envelope">The file content.</param>
        /// <param name="protection">The protection marker byte.</param>
        /// <param name="payload">The payload bytes.</param>
        /// <param name="error">What is wrong with the envelope, when it cannot be parsed.</param>
        /// <returns>True when the envelope parsed.</returns>
        internal static bool TryParse(byte[] envelope, out byte protection, out byte[] payload,
            out string? error)
        {
            protection = 0;
            payload = Array.Empty<byte>();

            if (envelope.Length < HEADER_LENGTH ||
                envelope[0] != MAGIC[0] || envelope[1] != MAGIC[1] ||
                envelope[2] != MAGIC[2] || envelope[3] != MAGIC[3])
            {
                error = "The file is not a secret envelope (bad magic); it is corrupt or was not written by this library.";
                return false;
            }

            if (envelope[4] > FORMAT_VERSION)
            {
                error = $"The envelope is format version {envelope[4]}; this build reads up to {FORMAT_VERSION}. " +
                        "It was written by a newer build.";
                return false;
            }

            protection = envelope[5];
            payload = envelope.AsSpan(HEADER_LENGTH).ToArray();
            error = null;
            return true;
        }

        #endregion
    }
}
