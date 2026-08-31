using System;
using System.Runtime.InteropServices;

namespace OutWit.Shared.Secrets.Provider.Linux
{
    /// <summary>
    /// libsecret password-API interop, source-generated. The vararg attribute tail is
    /// declared explicitly with one string attribute — the MSAL.Extensions style, well-trodden
    /// on x64 and AAPCS64. gboolean is a C int, declared as such rather than marshalled.
    /// GError domains are runtime-registered quarks, so classification compares against the
    /// quarks the libraries themselves report rather than hard-coded numbers.
    /// </summary>
    internal static partial class SecretStoreLibsecretNative
    {
        #region Constants

        internal const int SECRET_SCHEMA_NONE = 0;
        internal const int SECRET_SCHEMA_ATTRIBUTE_STRING = 0;

        // G_DBUS_ERROR codes (gio/gioenums.h).
        private const int G_DBUS_ERROR_SERVICE_UNKNOWN = 2;
        private const int G_DBUS_ERROR_NAME_HAS_NO_OWNER = 3;
        private const int G_DBUS_ERROR_NO_REPLY = 4;
        private const int G_DBUS_ERROR_ACCESS_DENIED = 9;
        private const int G_DBUS_ERROR_AUTH_FAILED = 10;
        private const int G_DBUS_ERROR_NO_SERVER = 11;
        private const int G_DBUS_ERROR_TIMEOUT = 12;
        private const int G_DBUS_ERROR_DISCONNECTED = 15;
        private const int G_DBUS_ERROR_TIMED_OUT = 20;

        // SecretError codes (libsecret/secret-types.h).
        private const int SECRET_ERROR_IS_LOCKED = 2;

        private const string LIBSECRET = "libsecret-1.so.0";
        private const string LIBGLIB = "libglib-2.0.so.0";
        private const string LIBGIO = "libgio-2.0.so.0";

        #endregion

        #region Fields

        private static uint m_dbusQuark;

        private static uint m_secretQuark;

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

        [LibraryImport(LIBSECRET)]
        internal static partial uint secret_error_get_quark();

        [LibraryImport(LIBGLIB)]
        internal static partial void g_error_free(IntPtr error);

        [LibraryImport(LIBGIO)]
        internal static partial uint g_dbus_error_quark();

        #endregion

        #region Tools

        /// <summary>
        /// Reads, classifies and frees a GError. False when there was none.
        /// </summary>
        /// <param name="error">The GError pointer from a call.</param>
        /// <param name="status">Where the error lands in the status model: D-Bus transport
        /// and locked-collection errors are the store not opening
        /// (<see cref="OutWit.Shared.Secrets.Providers.SecretStatus.Unavailable"/>), an
        /// access or authentication denial is
        /// <see cref="OutWit.Shared.Secrets.Providers.SecretStatus.Denied"/>, anything
        /// unrecognized is
        /// <see cref="OutWit.Shared.Secrets.Providers.SecretStatus.Failed"/> — with the
        /// domain and code always in the message for the case the table is wrong.</param>
        /// <param name="message">The error text with domain and code.</param>
        /// <returns>True when the call carried an error.</returns>
        internal static bool TryConsumeError(IntPtr error,
            out OutWit.Shared.Secrets.Providers.SecretStatus status, out string message)
        {
            status = OutWit.Shared.Secrets.Providers.SecretStatus.Failed;
            message = "";

            if (error == IntPtr.Zero)
                return false;

            try
            {
                GError value = Marshal.PtrToStructure<GError>(error);
                string text = Marshal.PtrToStringUTF8(value.Message) ?? "unknown GError";

                status = Classify(value.Domain, value.Code);
                message = $"{text} (domain {value.Domain}, code {value.Code})";
                return true;
            }
            finally
            {
                g_error_free(error);
            }
        }

        private static OutWit.Shared.Secrets.Providers.SecretStatus Classify(uint domain, int code)
        {
            if (domain != 0 && domain == DBusQuark())
            {
                return code switch
                {
                    G_DBUS_ERROR_SERVICE_UNKNOWN or G_DBUS_ERROR_NAME_HAS_NO_OWNER or
                    G_DBUS_ERROR_NO_REPLY or G_DBUS_ERROR_NO_SERVER or
                    G_DBUS_ERROR_TIMEOUT or G_DBUS_ERROR_DISCONNECTED or
                    G_DBUS_ERROR_TIMED_OUT
                        => OutWit.Shared.Secrets.Providers.SecretStatus.Unavailable,

                    G_DBUS_ERROR_ACCESS_DENIED or G_DBUS_ERROR_AUTH_FAILED
                        => OutWit.Shared.Secrets.Providers.SecretStatus.Denied,

                    _ => OutWit.Shared.Secrets.Providers.SecretStatus.Failed
                };
            }

            if (domain != 0 && domain == SecretQuark())
                return code == SECRET_ERROR_IS_LOCKED
                    ? OutWit.Shared.Secrets.Providers.SecretStatus.Unavailable
                    : OutWit.Shared.Secrets.Providers.SecretStatus.Failed;

            return OutWit.Shared.Secrets.Providers.SecretStatus.Failed;
        }

        private static uint DBusQuark()
        {
            if (m_dbusQuark == 0)
            {
                try
                {
                    m_dbusQuark = g_dbus_error_quark();
                }
                catch (DllNotFoundException)
                {
                    // No gio — classification falls back to Failed; the message still
                    // carries the numeric domain and code.
                }
            }

            return m_dbusQuark;
        }

        private static uint SecretQuark()
        {
            if (m_secretQuark == 0)
            {
                try
                {
                    m_secretQuark = secret_error_get_quark();
                }
                catch (DllNotFoundException)
                {
                    // As above.
                }
                catch (EntryPointNotFoundException)
                {
                    // Very old libsecret — as above.
                }
            }

            return m_secretQuark;
        }

        #endregion
    }
}
