using System;
using System.Runtime.InteropServices;

namespace OutWit.Shared.Secrets.Provider.File
{
    /// <summary>
    /// POSIX directory fsync: on Linux/macOS a rename is atomic but not durable until the
    /// containing directory's metadata is flushed — without this, a power loss inside the
    /// journal-commit window can resurface the previous value after StoreAsync already
    /// promised durability. Best-effort: a filesystem that refuses the open simply keeps
    /// its own guarantees.
    /// </summary>
    internal static partial class SecretStoreFileNative
    {
        #region Constants

        private const int O_RDONLY = 0;

        private const string LIBC = "libc";

        #endregion

        #region Functions

        [LibraryImport(LIBC, EntryPoint = "open", StringMarshalling = StringMarshalling.Utf8,
            SetLastError = true)]
        private static partial int Open(string path, int flags);

        [LibraryImport(LIBC, EntryPoint = "fsync", SetLastError = true)]
        private static partial int Fsync(int descriptor);

        [LibraryImport(LIBC, EntryPoint = "close", SetLastError = true)]
        private static partial int Close(int descriptor);

        /// <summary>
        /// Flushes a directory's metadata to disk, best-effort. No-op on Windows, where the
        /// rename goes through the NTFS journal.
        /// </summary>
        /// <param name="path">The directory.</param>
        internal static void FsyncDirectory(string path)
        {
            if (OperatingSystem.IsWindows())
                return;

            try
            {
                int descriptor = Open(path, O_RDONLY);
                if (descriptor < 0)
                    return;

                try
                {
                    Fsync(descriptor);
                }
                finally
                {
                    Close(descriptor);
                }
            }
            catch (DllNotFoundException)
            {
                // No loadable libc alias on this platform — the rename keeps whatever
                // durability the filesystem gives it.
            }
        }

        #endregion
    }
}
