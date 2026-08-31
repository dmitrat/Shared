using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Provider.File
{
    /// <summary>
    /// The explicitly-labelled file fallback — for deployments with no reachable OS credential
    /// store (containers, session-less services). Never selected automatically: configuration
    /// names it. One file per key with owner-only permissions verified at creation, atomic
    /// replace via temp-file-and-rename, DPAPI machine-scope protection on Windows and honest
    /// <see cref="SecretProtection.FileOnly"/> elsewhere.
    /// </summary>
    /// <remarks>
    /// What this does not defend against: an administrator, an attacker running as the owning
    /// account, an offline attack on a stolen disk or image, cloning. A host that requires
    /// better refuses to run on this store's <see cref="SecretStoreDescription.Protection"/>.
    /// </remarks>
    public sealed class SecretStoreFile : SecretStoreBase
    {
        #region Constants

        private const string EXTENSION = ".wsecret";

        private const string ENTROPY_PREFIX = "OutWit.Shared.Secrets/";

        #endregion

        #region Fields

        private readonly SemaphoreSlim m_lock = new SemaphoreSlim(1, 1);

        private readonly string m_directory;

        private readonly bool m_usePlatformKey;

        private readonly SecretStoreDescription m_description;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the store over a directory.
        /// </summary>
        /// <param name="options">Where the files live and how they are protected.</param>
        /// <exception cref="ArgumentNullException">The options are null.</exception>
        /// <exception cref="ArgumentException">The directory path is empty.</exception>
        public SecretStoreFile(SecretStoreFileOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(options.DirectoryPath))
                throw new ArgumentException("A directory path is required.", nameof(options));

            m_directory = Path.GetFullPath(options.DirectoryPath);
            m_usePlatformKey = options.UsePlatformKey && OperatingSystem.IsWindows();

            m_description = new SecretStoreDescription
            {
                Key = "File",
                Protection = m_usePlatformKey
                    ? SecretProtection.FileWithPlatformKey
                    : SecretProtection.FileOnly,
                CanWrite = true,
                Location = m_directory
            };
        }

        #endregion

        #region Functions

        /// <inheritdoc />
        protected override async Task<SecretOutcome> DoStoreAsync(string key,
            ReadOnlyMemory<byte> secret, CancellationToken token)
        {
            await m_lock.WaitAsync(token).ConfigureAwait(false);

            byte[]? payload = null;

            try
            {
                EnsureDirectory();

                payload = Protect(key, secret);
                byte[] envelope = SecretStoreFileFormat.Build(
                    m_usePlatformKey
                        ? SecretStoreFileFormat.PROTECTION_DPAPI_MACHINE
                        : SecretStoreFileFormat.PROTECTION_NONE,
                    payload);

                string path = MapPath(key);
                string temp = $"{path}.{Guid.NewGuid():N}.tmp";

                try
                {
                    using (FileStream stream = SecretStoreFilePermissions.CreateRestricted(temp))
                    {
                        await stream.WriteAsync(envelope, token).ConfigureAwait(false);
                        stream.Flush(true);
                    }

                    System.IO.File.Move(temp, path, true);
                }
                finally
                {
                    if (System.IO.File.Exists(temp))
                        System.IO.File.Delete(temp);
                }

                string? violation = SecretStoreFilePermissions.Verify(path);
                if (violation != null)
                    return new SecretOutcome
                    {
                        Status = SecretStatus.Failed,
                        Message = $"The permissions read back from '{path}' are wrong: {violation}"
                    };

                CleanupStaleTemp(path);

                return new SecretOutcome { Status = SecretStatus.Found };
            }
            catch (UnauthorizedAccessException ex)
            {
                return new SecretOutcome { Status = SecretStatus.Denied, Message = ex.Message };
            }
            catch (IOException ex)
            {
                return new SecretOutcome { Status = SecretStatus.Failed, Message = ex.Message };
            }
            finally
            {
                if (payload != null)
                    CryptographicOperations.ZeroMemory(payload);

                m_lock.Release();
            }
        }

        /// <inheritdoc />
        protected override async Task<SecretResult> DoReadAsync(string key, CancellationToken token)
        {
            string path = MapPath(key);

            try
            {
                if (!System.IO.File.Exists(path))
                    return new SecretResult { Status = SecretStatus.NotFound };

                byte[] envelope = await System.IO.File.ReadAllBytesAsync(path, token).ConfigureAwait(false);

                if (!SecretStoreFileFormat.TryParse(envelope, out byte protection,
                        out byte[] payload, out string? error))
                    return new SecretResult
                    {
                        Status = SecretStatus.Failed,
                        Message = $"'{path}': {error}"
                    };

                return Unprotect(key, path, protection, payload);
            }
            catch (UnauthorizedAccessException ex)
            {
                return new SecretResult { Status = SecretStatus.Denied, Message = ex.Message };
            }
            catch (IOException ex)
            {
                return new SecretResult { Status = SecretStatus.Failed, Message = ex.Message };
            }
        }

        /// <inheritdoc />
        protected override async Task<SecretOutcome> DoDeleteAsync(string key, CancellationToken token)
        {
            await m_lock.WaitAsync(token).ConfigureAwait(false);

            try
            {
                string path = MapPath(key);

                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);

                CleanupStaleTemp(path);

                return new SecretOutcome { Status = SecretStatus.NotFound };
            }
            catch (UnauthorizedAccessException ex)
            {
                return new SecretOutcome { Status = SecretStatus.Denied, Message = ex.Message };
            }
            catch (IOException ex)
            {
                return new SecretOutcome { Status = SecretStatus.Failed, Message = ex.Message };
            }
            finally
            {
                m_lock.Release();
            }
        }

        #endregion

        #region Tools

        /// <summary>
        /// Maps a key to its file name: '/' becomes '.', plus "-{first 8 hex of SHA-256(key)}"
        /// so the mapping stays injective on case-insensitive file systems, plus ".wsecret".
        /// Public so support tooling can locate a file.
        /// </summary>
        /// <param name="key">The secret's key — see <see cref="SecretKeys"/>.</param>
        /// <returns>The file name, without a directory.</returns>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="ArgumentException">The key breaks the <see cref="SecretKeys"/> rules.</exception>
        public static string MapFileName(string key)
        {
            SecretKeys.Validate(key);

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            return key.Replace('/', '.') + "-" +
                   Convert.ToHexString(hash, 0, 4).ToLowerInvariant() + EXTENSION;
        }

        private string MapPath(string key)
        {
            return Path.Combine(m_directory, MapFileName(key));
        }

        private void EnsureDirectory()
        {
            if (Directory.Exists(m_directory))
                return;

            if (OperatingSystem.IsWindows())
                Directory.CreateDirectory(m_directory);
            else
                Directory.CreateDirectory(m_directory,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        private void CleanupStaleTemp(string path)
        {
            if (!Directory.Exists(m_directory))
                return;

            string fileName = Path.GetFileName(path);

            foreach (string temp in Directory.GetFiles(m_directory, fileName + ".*.tmp"))
            {
                try
                {
                    System.IO.File.Delete(temp);
                }
                catch (IOException)
                {
                    // In use by a concurrent writer; its own cleanup will get it.
                }
                catch (UnauthorizedAccessException)
                {
                    // Not ours to delete; leave it.
                }
            }
        }

        private byte[] Protect(string key, ReadOnlyMemory<byte> secret)
        {
            if (m_usePlatformKey && OperatingSystem.IsWindows())
                return ProtectDpapi(key, secret);

            return secret.ToArray();
        }

        private SecretResult Unprotect(string key, string path, byte protection, byte[] payload)
        {
            if (protection == SecretStoreFileFormat.PROTECTION_NONE)
                return new SecretResult { Status = SecretStatus.Found, Secret = payload };

            if (protection == SecretStoreFileFormat.PROTECTION_DPAPI_MACHINE)
            {
                if (!OperatingSystem.IsWindows())
                    return new SecretResult
                    {
                        Status = SecretStatus.Unavailable,
                        Message = $"'{path}' is protected with Windows DPAPI and cannot be " +
                                  "opened on this platform."
                    };

                try
                {
                    return new SecretResult
                    {
                        Status = SecretStatus.Found,
                        Secret = UnprotectDpapi(key, payload)
                    };
                }
                catch (CryptographicException ex)
                {
                    return new SecretResult
                    {
                        Status = SecretStatus.Failed,
                        Message = $"DPAPI could not unprotect '{path}' — the file was moved " +
                                  $"from another machine, or is corrupt: {ex.Message}"
                    };
                }
            }

            return new SecretResult
            {
                Status = SecretStatus.Failed,
                Message = $"'{path}' carries an unknown protection marker ({protection})."
            };
        }

        [SupportedOSPlatform("windows")]
        private static byte[] ProtectDpapi(string key, ReadOnlyMemory<byte> secret)
        {
            byte[] plain = secret.ToArray();

            try
            {
                return ProtectedData.Protect(plain, Entropy(key),
                    DataProtectionScope.LocalMachine);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plain);
            }
        }

        [SupportedOSPlatform("windows")]
        private static byte[] UnprotectDpapi(string key, byte[] payload)
        {
            return ProtectedData.Unprotect(payload, Entropy(key),
                DataProtectionScope.LocalMachine);
        }

        private static byte[] Entropy(string key)
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes(ENTROPY_PREFIX + key));
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public override SecretStoreDescription Description => m_description;

        #endregion
    }
}
