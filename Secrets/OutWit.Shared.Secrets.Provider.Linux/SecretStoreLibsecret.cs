using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Provider.Linux
{
    /// <summary>
    /// Linux Secret Service provider via libsecret, for interactive desktop sessions with an
    /// unlocked keyring. The Secret Service is a <b>session</b> service: a systemd unit with
    /// no session, or a container, has no session bus and no unlocked keyring — the honest
    /// answer there is <see cref="SecretStatus.Unavailable"/>, never a silent fallback to a
    /// file. For those deployments the File provider is the intended, deliberate choice.
    /// </summary>
    /// <remarks>
    /// The libsecret password API is string-based, so the payload is stored
    /// <b>base64-encoded</b>; the encoded string is managed memory and cannot be erased —
    /// the same stated trade-off as the string extensions. Attribute matching is exact and
    /// case-sensitive, so the key maps to the attribute identically, with no suffix. The
    /// native calls are synchronous D-Bus round-trips and can stall on an unlock prompt;
    /// each operation runs on the thread pool so an awaiting UI thread is never blocked, but
    /// an in-flight call is not interruptible — the token cancels the wait, not the call.
    /// </remarks>
    [SupportedOSPlatform("linux")]
    public sealed class SecretStoreLibsecret : SecretStoreBase
    {
        #region Constants

        private const string SCHEMA_NAME = "com.outwit.secrets";

        private const string ATTRIBUTE_KEY = "key";

        private const string COLLECTION_DEFAULT = "default";

        private const string NO_LIBRARY =
            "libsecret-1.so.0 is not installed; there is no Secret Service to talk to. " +
            "On a desktop, install libsecret; on a headless or container deployment, " +
            "configure the File provider deliberately instead.";

        #endregion

        #region Fields

        private static readonly object LOCK = new object();

        private static IntPtr m_schema = IntPtr.Zero;

        private readonly SecretStoreDescription m_description = new SecretStoreDescription
        {
            Key = "Libsecret",
            Protection = SecretProtection.OperatingSystem,
            CanWrite = true,
            Location = "Secret Service (session keyring), schema '" + SCHEMA_NAME +
                       "', attribute key='{key}'; the value is base64 of the secret — " +
                       "inspect with: secret-tool lookup key '{key}'"
        };

        #endregion

        #region Functions

        /// <inheritdoc />
        protected override Task<SecretOutcome> DoStoreAsync(string key, byte[] secret,
            CancellationToken token)
        {
            string encoded = Convert.ToBase64String(secret);
            return Task.Run(() => StoreCore(key, encoded), token);
        }

        /// <inheritdoc />
        protected override Task<SecretResult> DoReadAsync(string key, CancellationToken token)
        {
            return Task.Run(() => ReadCore(key), token);
        }

        /// <inheritdoc />
        protected override Task<SecretOutcome> DoDeleteAsync(string key, CancellationToken token)
        {
            return Task.Run(() => DeleteCore(key), token);
        }

        private SecretOutcome StoreCore(string key, string encoded)
        {
            int stored = SecretStoreLibsecretNative.secret_password_store_sync(
                Schema(), COLLECTION_DEFAULT, Label(key), encoded, IntPtr.Zero,
                out IntPtr error, ATTRIBUTE_KEY, key, IntPtr.Zero);

            if (SecretStoreLibsecretNative.TryConsumeError(error,
                    out SecretStatus status, out string reason))
                return new SecretOutcome
                {
                    Status = status,
                    Message = $"The Secret Service refused the store of '{key}': {reason}"
                };

            if (stored == 0)
                return new SecretOutcome
                {
                    Status = SecretStatus.Failed,
                    Message = $"The Secret Service refused the store of '{key}' without an error."
                };

            return new SecretOutcome { Status = SecretStatus.Found };
        }

        private SecretResult ReadCore(string key)
        {
            IntPtr secretPtr = SecretStoreLibsecretNative.secret_password_lookup_sync(
                Schema(), IntPtr.Zero, out IntPtr error,
                ATTRIBUTE_KEY, key, IntPtr.Zero);

            if (SecretStoreLibsecretNative.TryConsumeError(error,
                    out SecretStatus status, out string reason))
                return new SecretResult
                {
                    Status = status,
                    Message = $"The Secret Service refused the read of '{key}': {reason}"
                };

            if (secretPtr == IntPtr.Zero)
                return new SecretResult { Status = SecretStatus.NotFound };

            string? encoded;

            try
            {
                encoded = Marshal.PtrToStringUTF8(secretPtr);
            }
            finally
            {
                SecretStoreLibsecretNative.secret_password_free(secretPtr);
            }

            if (string.IsNullOrEmpty(encoded))
                return new SecretResult
                {
                    Status = SecretStatus.Failed,
                    Message = $"The item for '{key}' carries an empty value; " +
                              "it was not written by this library."
                };

            try
            {
                return new SecretResult
                {
                    Status = SecretStatus.Found,
                    Secret = Convert.FromBase64String(encoded)
                };
            }
            catch (FormatException)
            {
                return new SecretResult
                {
                    Status = SecretStatus.Failed,
                    Message = $"The item for '{key}' is not base64; " +
                              "it was not written by this library."
                };
            }
        }

        private SecretOutcome DeleteCore(string key)
        {
            SecretStoreLibsecretNative.secret_password_clear_sync(
                Schema(), IntPtr.Zero, out IntPtr error,
                ATTRIBUTE_KEY, key, IntPtr.Zero);

            if (SecretStoreLibsecretNative.TryConsumeError(error,
                    out SecretStatus status, out string reason))
                return new SecretOutcome
                {
                    Status = status,
                    Message = $"The Secret Service refused the delete of '{key}': {reason}"
                };

            // False without an error means nothing matched — delete is idempotent.
            return new SecretOutcome { Status = SecretStatus.NotFound };
        }

        #endregion

        #region Tools

        /// <inheritdoc />
        protected override (SecretStatus Status, string Message)? MapException(Exception exception)
        {
            if (exception is DllNotFoundException or EntryPointNotFoundException)
                return (SecretStatus.Unavailable, NO_LIBRARY);

            return null;
        }

        private static string Label(string key)
        {
            return "OutWit.Shared.Secrets/" + key;
        }

        private static IntPtr Schema()
        {
            lock (LOCK)
            {
                if (m_schema == IntPtr.Zero)
                    m_schema = SecretStoreLibsecretNative.secret_schema_new(
                        SCHEMA_NAME, SecretStoreLibsecretNative.SECRET_SCHEMA_NONE,
                        ATTRIBUTE_KEY, SecretStoreLibsecretNative.SECRET_SCHEMA_ATTRIBUTE_STRING,
                        IntPtr.Zero);

                return m_schema;
            }
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public override SecretStoreDescription Description => m_description;

        #endregion
    }
}
