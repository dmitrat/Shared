using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Provider.MacOS
{
    /// <summary>
    /// macOS Keychain provider: generic passwords in the login keychain, service
    /// "com.outwit.secrets", account = the key. An existing item is <b>replaced in place</b>
    /// (<c>SecKeychainItemModifyAttributesAndData</c>) — no delete-then-add window in which a
    /// crash leaves nothing — and located <b>without decrypting it</b>, so overwriting a
    /// credential never triggers the ACL check reading one would. For user-facing
    /// applications; a daemon belongs in the System keychain, which this provider does not
    /// manage.
    /// </summary>
    /// <remarks>
    /// The keychain ACL names the binary that created an item. An item added by one binary
    /// and read by another prompts the user — and a prompt in a service is a hang. Keep the
    /// storing and the reading binary the same, or expect the prompt. The native calls are
    /// synchronous; each operation runs on the thread pool so an awaiting UI thread is never
    /// blocked, but an in-flight call is not interruptible — the token cancels the wait, not
    /// the call.
    /// </remarks>
    [SupportedOSPlatform("macos")]
    public sealed class SecretStoreKeychain : SecretStoreBase
    {
        #region Constants

        private const string SERVICE = "com.outwit.secrets";

        private const int STORE_ATTEMPTS = 3;

        private const string NO_FRAMEWORK =
            "The Security framework is not loadable; there is no Keychain here.";

        #endregion

        #region Fields

        private static readonly byte[] SERVICE_BYTES = Encoding.UTF8.GetBytes(SERVICE);

        private readonly SecretStoreDescription m_description = new SecretStoreDescription
        {
            Key = "Keychain",
            Protection = SecretProtection.OperatingSystem,
            CanWrite = true,
            Location = "login Keychain (Keychain Access → login → search '" + SERVICE +
                       "'), service='" + SERVICE + "', account='{key}'"
        };

        #endregion

        #region Functions

        /// <inheritdoc />
        protected override Task<SecretOutcome> DoStoreAsync(string key, byte[] secret,
            CancellationToken token)
        {
            return Task.Run(() => StoreCore(key, secret), token);
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

        private SecretOutcome StoreCore(string key, byte[] secret)
        {
            byte[] account = Encoding.UTF8.GetBytes(key);

            for (int attempt = 0; attempt < STORE_ATTEMPTS; attempt++)
            {
                int status = FindRef(account, out IntPtr item);

                if (status == SecretStoreKeychainNative.ERR_SEC_SUCCESS)
                {
                    try
                    {
                        status = SecretStoreKeychainNative.SecKeychainItemModifyAttributesAndData(
                            item, IntPtr.Zero, (uint)secret.Length, secret);
                    }
                    finally
                    {
                        if (item != IntPtr.Zero)
                            SecretStoreKeychainNative.CFRelease(item);
                    }

                    if (status == SecretStoreKeychainNative.ERR_SEC_SUCCESS)
                        return new SecretOutcome { Status = SecretStatus.Found };

                    // A concurrent delete won between find and modify: retry as an add.
                    if (status == SecretStoreKeychainNative.ERR_SEC_ITEM_NOT_FOUND)
                        continue;

                    return DescribeStoreOutcome(key, status);
                }

                if (status != SecretStoreKeychainNative.ERR_SEC_ITEM_NOT_FOUND)
                    return DescribeStoreOutcome(key, status);

                status = SecretStoreKeychainNative.SecKeychainAddGenericPassword(
                    IntPtr.Zero,
                    (uint)SERVICE_BYTES.Length, SERVICE_BYTES,
                    (uint)account.Length, account,
                    (uint)secret.Length, secret,
                    out IntPtr added);

                if (added != IntPtr.Zero)
                    SecretStoreKeychainNative.CFRelease(added);

                if (status == SecretStoreKeychainNative.ERR_SEC_SUCCESS)
                    return new SecretOutcome { Status = SecretStatus.Found };

                // A concurrent writer added the item between find and add: retry via modify.
                if (status != SecretStoreKeychainNative.ERR_SEC_DUPLICATE_ITEM)
                    return DescribeStoreOutcome(key, status);
            }

            return new SecretOutcome
            {
                Status = SecretStatus.Failed,
                Message = $"The store of '{key}' kept racing a concurrent writer " +
                          $"({STORE_ATTEMPTS} attempts)."
            };
        }

        private SecretResult ReadCore(string key)
        {
            byte[] account = Encoding.UTF8.GetBytes(key);

            int status = SecretStoreKeychainNative.SecKeychainFindGenericPassword(
                IntPtr.Zero,
                (uint)SERVICE_BYTES.Length, SERVICE_BYTES,
                (uint)account.Length, account,
                out uint length, out IntPtr data, out IntPtr item);

            if (status == SecretStoreKeychainNative.ERR_SEC_ITEM_NOT_FOUND)
                return new SecretResult { Status = SecretStatus.NotFound };

            if (status != SecretStoreKeychainNative.ERR_SEC_SUCCESS)
                return DescribeResult("read", key, status);

            try
            {
                if (length == 0)
                    return new SecretResult
                    {
                        Status = SecretStatus.Failed,
                        Message = $"The item for '{key}' carries an empty value; " +
                                  "it was not written by this library."
                    };

                byte[] secret = new byte[length];
                Marshal.Copy(data, secret, 0, (int)length);

                return new SecretResult
                {
                    Status = SecretStatus.Found,
                    Secret = secret
                };
            }
            finally
            {
                SecretStoreKeychainNative.SecKeychainItemFreeContent(IntPtr.Zero, data);

                if (item != IntPtr.Zero)
                    SecretStoreKeychainNative.CFRelease(item);
            }
        }

        private SecretOutcome DeleteCore(string key)
        {
            byte[] account = Encoding.UTF8.GetBytes(key);

            int status = FindRef(account, out IntPtr item);

            if (status == SecretStoreKeychainNative.ERR_SEC_ITEM_NOT_FOUND)
                return new SecretOutcome { Status = SecretStatus.NotFound };

            if (status != SecretStoreKeychainNative.ERR_SEC_SUCCESS)
                return DescribeOutcome("delete", key, status);

            try
            {
                status = SecretStoreKeychainNative.SecKeychainItemDelete(item);
            }
            finally
            {
                if (item != IntPtr.Zero)
                    SecretStoreKeychainNative.CFRelease(item);
            }

            if (status != SecretStoreKeychainNative.ERR_SEC_SUCCESS)
                return DescribeOutcome("delete", key, status);

            return new SecretOutcome { Status = SecretStatus.NotFound };
        }

        #endregion

        #region Tools

        /// <inheritdoc />
        protected override (SecretStatus Status, string Message)? MapException(Exception exception)
        {
            if (exception is DllNotFoundException or EntryPointNotFoundException)
                return (SecretStatus.Unavailable, NO_FRAMEWORK);

            return null;
        }

        private static int FindRef(byte[] account, out IntPtr item)
        {
            return SecretStoreKeychainNative.SecKeychainFindGenericPasswordRef(
                IntPtr.Zero,
                (uint)SERVICE_BYTES.Length, SERVICE_BYTES,
                (uint)account.Length, account,
                IntPtr.Zero, IntPtr.Zero,
                out item);
        }

        private static SecretStatus MapStatus(int status)
        {
            return status switch
            {
                SecretStoreKeychainNative.ERR_SEC_ITEM_NOT_FOUND => SecretStatus.NotFound,
                SecretStoreKeychainNative.ERR_SEC_AUTH_FAILED => SecretStatus.Denied,
                SecretStoreKeychainNative.ERR_SEC_INTERACTION_NOT_ALLOWED => SecretStatus.Unavailable,
                SecretStoreKeychainNative.ERR_SEC_NO_DEFAULT_KEYCHAIN => SecretStatus.Unavailable,
                SecretStoreKeychainNative.ERR_SEC_NOT_AVAILABLE => SecretStatus.Unavailable,
                _ => SecretStatus.Failed
            };
        }

        private static string DescribeStatus(string operation, string key, int status)
        {
            return $"Keychain refused the {operation} of '{SERVICE}'/'{key}': OSStatus {status}.";
        }

        private static SecretOutcome DescribeOutcome(string operation, string key, int status)
        {
            return new SecretOutcome
            {
                Status = MapStatus(status),
                Message = DescribeStatus(operation, key, status)
            };
        }

        private static SecretOutcome DescribeStoreOutcome(string key, int status)
        {
            SecretStatus mapped = MapStatus(status);

            return new SecretOutcome
            {
                // A write that "was not found" wrote nothing and must never look like success.
                Status = mapped == SecretStatus.NotFound ? SecretStatus.Failed : mapped,
                Message = DescribeStatus("store", key, status)
            };
        }

        private static SecretResult DescribeResult(string operation, string key, int status)
        {
            return new SecretResult
            {
                Status = MapStatus(status),
                Message = DescribeStatus(operation, key, status)
            };
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public override SecretStoreDescription Description => m_description;

        #endregion
    }
}
