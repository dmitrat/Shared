using System;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Shared.Secrets.Providers
{
    /// <summary>
    /// The abstraction's promises, enforced in one place so every provider sees the same
    /// thing: key validation, the size limit, the empty-secret refusal, and the read-only
    /// gate. Providers implement the Do* methods and nothing else on this path.
    /// </summary>
    public abstract class SecretStoreBase : ISecretStore
    {
        #region Constants

        /// <summary>
        /// Maximum secret size in bytes — comfortably inside every platform's limit and far
        /// above any credential this is for. This subsystem stores credentials, not
        /// certificates, key files or blobs.
        /// </summary>
        public const int MAX_SECRET_SIZE = 1024;

        #endregion

        #region Functions

        /// <summary>
        /// Stores or replaces a secret after the shared checks: an over-limit or empty secret
        /// is an operational failure, not an exception; a read-only store answers
        /// <see cref="SecretStatus.Denied"/>.
        /// </summary>
        /// <param name="key">The secret's key — see <see cref="SecretKeys"/>.</param>
        /// <param name="secret">The secret bytes.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The outcome; <see cref="SecretStatus.Found"/> on success.</returns>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="ArgumentException">The key breaks the <see cref="SecretKeys"/> rules.</exception>
        public Task<SecretOutcome> StoreAsync(string key, ReadOnlyMemory<byte> secret,
            CancellationToken token = default)
        {
            SecretKeys.Validate(key);
            token.ThrowIfCancellationRequested();

            if (!Description.CanWrite)
                return Task.FromResult(new SecretOutcome
                {
                    Status = SecretStatus.Denied,
                    Message = $"The '{Description.Key}' store is read-only."
                });

            if (secret.IsEmpty)
                return Task.FromResult(new SecretOutcome
                {
                    Status = SecretStatus.Failed,
                    Message = "An empty secret cannot be stored; \"\" and absent must never be confused. " +
                              "To remove a secret, delete it."
                });

            if (secret.Length > MAX_SECRET_SIZE)
                return Task.FromResult(new SecretOutcome
                {
                    Status = SecretStatus.Failed,
                    Message = $"The secret is {secret.Length} bytes; the limit is {MAX_SECRET_SIZE}. " +
                              "This store is for credentials, not certificates, key files or blobs."
                });

            return DoStoreAsync(key, secret, token);
        }

        /// <summary>
        /// Reads a secret after key validation.
        /// </summary>
        /// <param name="key">The secret's key — see <see cref="SecretKeys"/>.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The result; the secret bytes only when the status is <see cref="SecretStatus.Found"/>.</returns>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="ArgumentException">The key breaks the <see cref="SecretKeys"/> rules.</exception>
        public Task<SecretResult> ReadAsync(string key, CancellationToken token = default)
        {
            SecretKeys.Validate(key);
            token.ThrowIfCancellationRequested();

            return DoReadAsync(key, token);
        }

        /// <summary>
        /// Removes a secret after key validation. Removing one that is not there succeeds;
        /// a read-only store answers <see cref="SecretStatus.Denied"/>.
        /// </summary>
        /// <param name="key">The secret's key — see <see cref="SecretKeys"/>.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The outcome; <see cref="SecretStatus.NotFound"/> on success.</returns>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="ArgumentException">The key breaks the <see cref="SecretKeys"/> rules.</exception>
        public Task<SecretOutcome> DeleteAsync(string key, CancellationToken token = default)
        {
            SecretKeys.Validate(key);
            token.ThrowIfCancellationRequested();

            if (!Description.CanWrite)
                return Task.FromResult(new SecretOutcome
                {
                    Status = SecretStatus.Denied,
                    Message = $"The '{Description.Key}' store is read-only."
                });

            return DoDeleteAsync(key, token);
        }

        /// <summary>
        /// Provider store implementation. The key is valid, the secret is non-empty and inside
        /// the size limit, and the store is writable.
        /// </summary>
        /// <param name="key">The validated key.</param>
        /// <param name="secret">The secret bytes.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The outcome.</returns>
        protected abstract Task<SecretOutcome> DoStoreAsync(string key, ReadOnlyMemory<byte> secret,
            CancellationToken token);

        /// <summary>
        /// Provider read implementation. The key is valid.
        /// </summary>
        /// <param name="key">The validated key.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The result.</returns>
        protected abstract Task<SecretResult> DoReadAsync(string key, CancellationToken token);

        /// <summary>
        /// Provider delete implementation. The key is valid and the store is writable.
        /// </summary>
        /// <param name="key">The validated key.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The outcome.</returns>
        protected abstract Task<SecretOutcome> DoDeleteAsync(string key, CancellationToken token);

        #endregion

        #region Properties

        /// <summary>
        /// What this store is and how well it protects.
        /// </summary>
        public abstract SecretStoreDescription Description { get; }

        #endregion
    }
}
