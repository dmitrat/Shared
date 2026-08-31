using System;
using System.Runtime.InteropServices;

namespace OutWit.Shared.Secrets.Provider.Linux
{
    /// <summary>
    /// libsecret password-API interop, source-generated. The vararg attribute tail is
    /// declared explicitly with one string attribute — the MSAL.Extensions style, well-trodden
    /// on x64 and AAPCS64. gboolean is a C int, declared as such rather than marshalled.
    /// </summary>
    internal static partial class SecretStoreLibsecretNative
    {
        #region Constants

        internal const int SECRET_SCHEMA_NONE = 0;
        internal const int SECRET_SCHEMA_ATTRIBUTE_STRING = 0;

        private const string LIBSECRET = "libsecret-1.so.0";
        private const string LIBGLIB = "libglib-2.0.so.0";

        #endregion

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        internal struct GError
        {
            public uint Domain;
            public int Code;
            public IntPtr Message;
        }

        #endregion

        #region Functions

        [LibraryImport(LIBSECRET, StringMarshalling = StringMarshalling.Utf8)]
        internal static partial IntPtr secret_schema_new(string name, int flags,
            string attribute1, int type1, IntPtr terminator);

        [LibraryImport(LIBSECRET, StringMarshalling = StringMarshalling.Utf8)]
        internal static partial int secret_password_store_sync(IntPtr schema, string collection,
            string label, string password, IntPtr cancellable, out IntPtr error,
            string attribute1, string value1, IntPtr terminator);

        [LibraryImport(LIBSECRET, StringMarshalling = StringMarshalling.Utf8)]
        internal static partial IntPtr secret_password_lookup_sync(IntPtr schema,
            IntPtr cancellable, out IntPtr error,
            string attribute1, string value1, IntPtr terminator);

        [LibraryImport(LIBSECRET, StringMarshalling = StringMarshalling.Utf8)]
        internal static partial int secret_password_clear_sync(IntPtr schema,
            IntPtr cancellable, out IntPtr error,
            string attribute1, string value1, IntPtr terminator);

        [LibraryImport(LIBSECRET)]
        internal static partial void secret_password_free(IntPtr password);

        [LibraryImport(LIBGLIB)]
        internal static partial void g_error_free(IntPtr error);

        #endregion

        #region Tools

        /// <summary>
        /// Reads and frees a GError. Null when there was none.
        /// </summary>
        /// <param name="error">The GError pointer from a call.</param>
        /// <returns>The error message, or null when the call carried no error.</returns>
        internal static string? ConsumeError(IntPtr error)
        {
            if (error == IntPtr.Zero)
                return null;

            try
            {
                GError value = Marshal.PtrToStructure<GError>(error);
                string message = Marshal.PtrToStringUTF8(value.Message) ?? "unknown GError";
                return $"{message} (domain {value.Domain}, code {value.Code})";
            }
            finally
            {
                g_error_free(error);
            }
        }

        #endregion
    }
}
