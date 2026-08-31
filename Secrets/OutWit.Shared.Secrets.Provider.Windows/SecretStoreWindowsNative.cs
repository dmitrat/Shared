using System;
using System.Runtime.InteropServices;

namespace OutWit.Shared.Secrets.Provider.Windows
{
    /// <summary>
    /// advapi32 Credential Manager interop. Source-generated (<see cref="LibraryImportAttribute"/>)
    /// so the provider is NativeAOT-clean; the source generator does not marshal LPWStr strings
    /// inside structs, so <see cref="CREDENTIALW"/> is blittable with <see cref="IntPtr"/> fields
    /// and the strings are marshalled explicitly around the calls.
    /// </summary>
    internal static partial class SecretStoreWindowsNative
    {
        #region Constants

        internal const uint CRED_TYPE_GENERIC = 1;
        internal const uint CRED_PERSIST_LOCAL_MACHINE = 2;

        internal const int ERROR_ACCESS_DENIED = 5;
        internal const int ERROR_NOT_FOUND = 1168;
        internal const int ERROR_NO_SUCH_LOGON_SESSION = 1312;

        #endregion

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        internal struct CREDENTIALW
        {
            public uint Flags;
            public uint Type;
            public IntPtr TargetName;
            public IntPtr Comment;
            public uint LastWrittenLow;
            public uint LastWrittenHigh;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public IntPtr TargetAlias;
            public IntPtr UserName;
        }

        #endregion

        #region Functions

        [LibraryImport("advapi32.dll", EntryPoint = "CredWriteW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CredWrite(ref CREDENTIALW credential, uint flags);

        [LibraryImport("advapi32.dll", EntryPoint = "CredReadW", SetLastError = true,
            StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CredRead(string targetName, uint type, uint flags,
            out IntPtr credential);

        [LibraryImport("advapi32.dll", EntryPoint = "CredDeleteW", SetLastError = true,
            StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CredDelete(string targetName, uint type, uint flags);

        [LibraryImport("advapi32.dll", EntryPoint = "CredFree")]
        internal static partial void CredFree(IntPtr buffer);

        #endregion
    }
}
