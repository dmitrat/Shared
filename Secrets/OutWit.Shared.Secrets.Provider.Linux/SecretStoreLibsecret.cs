using System;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
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
    /// case-sensitive, so the key maps to the attribute identically, with no suffix.
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
        protected override Task<SecretOutcome> DoStoreAsync(string key, ReadOnlyMemory<byte> secret,
            CancellationToken token)
        {
            byte[] buffer = secret.ToArray();

            try
            {
                string encoded = Convert.ToBase64String(buffer);

                int stored = SecretStoreLibsecretNative.secret_password_store_sync(
                    Schema(), COLLECTION_DEFAULT, Label(key), encoded, IntPtr.Zero,
                    out IntPtr error, ATTRIBUTE_KEY, key, IntPtr.Zero);

                string? failure = SecretStoreLibsecretNative.ConsumeError(error);
                if (failure != null)
                    return Task.FromResult(new SecretOutcome
                    {
                        Status = SecretStatus.Unavailable,
                        Message = $"The Secret Service refused the store of '{key}': {failure}"
                    });

                if (stored == 0)
                    return Task.FromResult(new SecretOutcome
                    {
                        Status = SecretStatus.Failed,
                        Message = $"The Secret Service refused the store of '{key}' without an error."
                    });

                return Task.FromResult(new SecretOutcome { Status = SecretStatus.Found });
            }
            catch (DllNotFoundException)
            {
                return Task.FromResult(new SecretOutcome
                {
                    Status = SecretStatus.Unavailable,
                    Message = NO_LIBRARY
                });
            }
            finally
            {
                CryptographicOperations.ZeroMemory(buffer);
            }
        }

        /// <inheritdoc />
        protected override Task<SecretResult> DoReadAsync(string key, CancellationToken token)
        {
            try
            {
                IntPtr secretPtr = SecretStoreLibsecretNative.secret_password_lookup_sync(
                    Schema(), IntPtr.Zero, out IntPtr error,
                    ATTRIBUTE_KEY, key, IntPtr.Zero);

                string? failure = SecretStoreLibsecretNative.ConsumeError(error);
                if (failure != null)
                    return Task.FromResult(new SecretResult
                    {
                        Status = SecretStatus.Unavailable,
                        Message = $"The Secret Service refused the read of '{key}': {failure}"
                    });

                if (secretPtr == IntPtr.Zero)
                    return Task.FromResult(new SecretResult { Status = SecretStatus.NotFound });

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
                    return Task.FromResult(new SecretResult
                    {
                        Status = SecretStatus.Failed,
                        Message = $"The item for '{key}' carries an empty value; " +
                                  "it was not written by this library."
                    });

                try
                {
                    return Task.FromResult(new SecretResult
                    {
                        Status = SecretStatus.Found,
                        Secret = Convert.FromBase64String(encoded)
                    });
                }
                catch (FormatException)
                {
                    return Task.FromResult(new SecretResult
                    {
                        Status = SecretStatus.Failed,
                        Message = $"The item for '{key}' is not base64; " +
                                  "it was not written by this library."
                    });
                }
            }
            catch (DllNotFoundException)
            {
                return Task.FromResult(new SecretResult
                {
                    Status = SecretStatus.Unavailable,
                    Message = NO_LIBRARY
                });
            }
        }

        /// <inheritdoc />
        protected override Task<SecretOutcome> DoDeleteAsync(string key, CancellationToken token)
        {
            try
            {
                SecretStoreLibsecretNative.secret_password_clear_sync(
                    Schema(), IntPtr.Zero, out IntPtr error,
                    ATTRIBUTE_KEY, key, IntPtr.Zero);

                string? failure = SecretStoreLibsecretNative.ConsumeError(error);
                if (failure != null)
                    return Task.FromResult(new SecretOutcome
                    {
                        Status = SecretStatus.Unavailable,
                        Message = $"The Secret Service refused the delete of '{key}': {failure}"
                    });

                // False without an error means nothing matched — delete is idempotent.
                return Task.FromResult(new SecretOutcome { Status = SecretStatus.NotFound });
            }
            catch (DllNotFoundException)
            {
                return Task.FromResult(new SecretOutcome
                {
                    Status = SecretStatus.Unavailable,
                    Message = NO_LIBRARY
                });
            }
        }

        #endregion

        #region Tools

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
