using System;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Shared.Secrets.Providers
{
    /// <summary>
    /// Long-lived credentials — tokens, keys, passphrases — held by the operating system rather
    /// than by a file this process wrote. One value per key; keys are namespaced by the caller
    /// as "{Product}/{Purpose}" — see <see cref="SecretKeys"/>. Not for certificates, key files
    /// or blobs: secrets above <see cref="SecretStoreBase.MAX_SECRET_SIZE"/> bytes are refused
    /// with a named failure rather than a surprise on one platform.
    /// </summary>
    /// <remarks>
    /// Nothing on this path throws for an expected condition: a missing secret, a locked
    /// keyring and a denied read are all answers, carried in the status. Exceptions are
    /// reserved for programming errors — a null key, a key that breaks the
    /// <see cref="SecretKeys"/> rules.
    /// </remarks>
    public interface ISecretStore
    {
        /// <summary>
        /// What this store is and how well it protects. A host logs this once at startup.
        /// </summary>
        SecretStoreDescription Description { get; }

        /// <summary>
        /// Stores or replaces a secret. Replacement is atomic — a concurrent or interrupted
        /// write leaves the old value or the new one, never a mixture and never nothing — and
        /// the value is durable before the call returns.
        /// </summary>
        /// <param name="key">The secret's key — see <see cref="SecretKeys"/>.</param>
        /// <param name="secret">The secret bytes; the caller may clear its buffer after the call.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The outcome; <see cref="SecretStatus.Found"/> on success.</returns>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="ArgumentException">The key breaks the <see cref="SecretKeys"/> rules.</exception>
        Task<SecretOutcome> StoreAsync(string key, ReadOnlyMemory<byte> secret,
            CancellationToken token = default);

        /// <summary>
        /// Reads a secret. <see cref="SecretStatus.NotFound"/> is the only "not provisioned";
        /// a store that will not open answers <see cref="SecretStatus.Unavailable"/>, never null.
        /// </summary>
        /// <param name="key">The secret's key — see <see cref="SecretKeys"/>.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The result; the secret bytes only when the status is <see cref="SecretStatus.Found"/>.</returns>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="ArgumentException">The key breaks the <see cref="SecretKeys"/> rules.</exception>
        Task<SecretResult> ReadAsync(string key, CancellationToken token = default);

        /// <summary>
        /// Removes a secret. Removing one that is not there succeeds — uninstall must be
        /// idempotent.
        /// </summary>
        /// <param name="key">The secret's key — see <see cref="SecretKeys"/>.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The outcome; <see cref="SecretStatus.NotFound"/> on success.</returns>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="ArgumentException">The key breaks the <see cref="SecretKeys"/> rules.</exception>
        Task<SecretOutcome> DeleteAsync(string key, CancellationToken token = default);
    }
}
