using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Provider.Windows
{
    /// <summary>
    /// Windows Credential Manager provider: generic credentials via advapi32, in the vault of
    /// the account the process runs as. An interactive user cannot enumerate a service
    /// account's vault — for a service-plus-shell product that containment is the point.
    /// CRED_PERSIST_LOCAL_MACHINE means "this user, on this machine, and do not roam" — there
    /// is no machine-wide scope.
    /// </summary>
    /// <remarks>
    /// Whatever provisions a credential at install time must write it as the account that will
    /// read it: an installer running elevated writes into the administrator's vault by default,
    /// and the resulting service cannot read its own credential. Verify with a read, as that
    /// account, before reporting success. The native calls are synchronous; each operation runs
    /// on the thread pool so an awaiting UI thread is never blocked, but an in-flight platform
    /// call is not interruptible — the token cancels the wait, not the call.
    /// </remarks>
    [SupportedOSPlatform("windows")]
    public sealed class SecretStoreWindows : SecretStoreBase
    {
        #region Constants

        private const string COMMENT = "Stored by OutWit.Shared.Secrets";

        private const string USER_NAME = "OutWit.Shared.Secrets";

        #endregion

        #region Fields

        private readonly SecretStoreDescription m_description = new SecretStoreDescription
        {
            Key = "Windows",
            Protection = SecretProtection.OperatingSystem,
            CanWrite = true,
            Location = "Credential Manager → Windows Credentials → Generic Credentials, " +
                       "in the vault of the account this process runs as; " +
                       "target name = '{key}#{first 8 hex of SHA-256(key)}'"
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
            string target = MapKey(key);

            IntPtr blob = IntPtr.Zero;
            IntPtr targetPtr = IntPtr.Zero;
            IntPtr commentPtr = IntPtr.Zero;
            IntPtr userPtr = IntPtr.Zero;

            try
            {
                blob = Marshal.AllocCoTaskMem(secret.Length);
                Marshal.Copy(secret, 0, blob, secret.Length);

                targetPtr = Marshal.StringToCoTaskMemUni(target);
                commentPtr = Marshal.StringToCoTaskMemUni(COMMENT);
                userPtr = Marshal.StringToCoTaskMemUni(USER_NAME);

                var credential = new SecretStoreWindowsNative.CREDENTIALW
                {
                    Flags = 0,
                    Type = SecretStoreWindowsNative.CRED_TYPE_GENERIC,
                    TargetName = targetPtr,
                    Comment = commentPtr,
                    CredentialBlobSize = (uint)secret.Length,
                    CredentialBlob = blob,
                    Persist = SecretStoreWindowsNative.CRED_PERSIST_LOCAL_MACHINE,
                    AttributeCount = 0,
                    Attributes = IntPtr.Zero,
                    TargetAlias = IntPtr.Zero,
                    UserName = userPtr
                };

                if (!SecretStoreWindowsNative.CredWrite(ref credential, 0))
                {
                    int error = Marshal.GetLastWin32Error();
                    SecretStatus status = MapError(error);

                    return new SecretOutcome
                    {
                        // MapError is a read-oriented table; a write that "was not found"
                        // wrote nothing and must never look like success.
                        Status = status == SecretStatus.NotFound ? SecretStatus.Failed : status,
                        Message = Describe("store", error, target)
                    };
                }

                return new SecretOutcome { Status = SecretStatus.Found };
            }
            finally
            {
                if (blob != IntPtr.Zero)
                {
                    for (int i = 0; i < secret.Length; i++)
                        Marshal.WriteByte(blob, i, 0);

                    Marshal.FreeCoTaskMem(blob);
                }

                if (targetPtr != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(targetPtr);

                if (commentPtr != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(commentPtr);

                if (userPtr != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(userPtr);
            }
        }

        private SecretResult ReadCore(string key)
        {
            string target = MapKey(key);

            if (!SecretStoreWindowsNative.CredRead(target,
                    SecretStoreWindowsNative.CRED_TYPE_GENERIC, 0, out IntPtr credentialPtr))
            {
                int error = Marshal.GetLastWin32Error();

                if (error == SecretStoreWindowsNative.ERROR_NOT_FOUND)
                    return new SecretResult { Status = SecretStatus.NotFound };

                return new SecretResult
                {
                    Status = MapError(error),
                    Message = Describe("read", error, target)
                };
            }

            try
            {
                var credential = Marshal.PtrToStructure<SecretStoreWindowsNative.CREDENTIALW>(credentialPtr);

                if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0)
                    return new SecretResult
                    {
                        Status = SecretStatus.Failed,
                        Message = $"The credential '{target}' carries an empty blob; " +
                                  "it was not written by this library."
                    };

                int length = checked((int)credential.CredentialBlobSize);
                byte[] secret = new byte[length];
                Marshal.Copy(credential.CredentialBlob, secret, 0, length);

                return new SecretResult
                {
                    Status = SecretStatus.Found,
                    Secret = secret
                };
            }
            finally
            {
                SecretStoreWindowsNative.CredFree(credentialPtr);
            }
        }

        private SecretOutcome DeleteCore(string key)
        {
            string target = MapKey(key);

            if (!SecretStoreWindowsNative.CredDelete(target,
                    SecretStoreWindowsNative.CRED_TYPE_GENERIC, 0))
            {
                int error = Marshal.GetLastWin32Error();

                if (error != SecretStoreWindowsNative.ERROR_NOT_FOUND)
                    return new SecretOutcome
                    {
                        Status = MapError(error),
                        Message = Describe("delete", error, target)
                    };
            }

            return new SecretOutcome { Status = SecretStatus.NotFound };
        }

        #endregion

        #region Tools

        /// <summary>
        /// Maps a key to its Credential Manager target name:
        /// "{key}#{<see cref="SecretKeys.Fingerprint"/>}". Target names are case-insensitive
        /// on Windows, so an identity mapping is not injective; the suffix keeps two keys
        /// differing only in case apart, while the prefix keeps the entry findable in the
        /// Credential Manager UI. Public so support tooling can locate an entry.
        /// </summary>
        /// <param name="key">The secret's key — see <see cref="SecretKeys"/>.</param>
        /// <returns>The target name.</returns>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="ArgumentException">The key breaks the <see cref="SecretKeys"/> rules.</exception>
        public static string MapKey(string key)
        {
            return key + "#" + SecretKeys.Fingerprint(key);
        }

        private static SecretStatus MapError(int error)
        {
            return error switch
            {
                SecretStoreWindowsNative.ERROR_NOT_FOUND => SecretStatus.NotFound,
                SecretStoreWindowsNative.ERROR_ACCESS_DENIED => SecretStatus.Denied,
                SecretStoreWindowsNative.ERROR_NO_SUCH_LOGON_SESSION => SecretStatus.Unavailable,
                _ => SecretStatus.Failed
            };
        }

        private static string Describe(string operation, int error, string target)
        {
            string reason = new Win32Exception(error).Message;
            return $"Credential Manager refused the {operation} of '{target}': " +
                   $"Win32 error {error} ({reason}). The vault searched is the one of the " +
                   "account this process runs as.";
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public override SecretStoreDescription Description => m_description;

        #endregion
    }
}
