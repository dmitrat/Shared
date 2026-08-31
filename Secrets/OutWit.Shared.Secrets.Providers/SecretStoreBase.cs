using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Shared.Secrets.Providers
{
    /// <summary>
    /// The abstraction's promises, enforced in one place so every provider sees the same
    /// thing: key validation, the size limit, the empty-secret refusal, the read-only gate,
    /// the exception-to-status guard, the store-buffer zeroing, and the outcome
    /// normalizations. Providers implement the Do* methods and nothing else on this path.
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

        /// <summary>
        /// The key <see cref="CheckAsync"/> probes with. Reserved; never stored by the library.
        /// </summary>
        public const string PROBE_KEY = "OutWit.Shared.Secrets/Probe";

        #endregion

        #region Functions

        /// <summary>
        /// Stores or replaces a secret after the shared checks: an over-limit or empty secret
        /// is an operational failure, not an exception; a read-only store answers
        /// <see cref="SecretStatus.Denied"/>. The secret buffer handed to the provider is
        /// cleared before this method returns.
        /// </summary>
        /// <param name="key">The secret's key — see <see cref="SecretKeys"/>.</param>
        /// <param name="secret">The secret bytes.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The outcome; <see cref="SecretStatus.Found"/> on success — never
        /// <see cref="SecretStatus.NotFound"/>, which a store cannot legitimately produce.</returns>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="ArgumentException">The key breaks the <see cref="SecretKeys"/> rules.</exception>
        public async Task<SecretOutcome> StoreAsync(string key, ReadOnlyMemory<byte> secret,
            CancellationToken token = default)
        {
            SecretKeys.Validate(key);
            token.ThrowIfCancellationRequested();

            if (!Description.CanWrite)
                return DeniedReadOnly();

            if (secret.IsEmpty)
                return new SecretOutcome
                {
                    Status = SecretStatus.Failed,
                    Message = "An empty secret cannot be stored; \"\" and absent must never be confused. " +
                              "To remove a secret, delete it."
                };

            if (secret.Length > MAX_SECRET_SIZE)
                return new SecretOutcome
                {
                    Status = SecretStatus.Failed,
                    Message = $"The secret is {secret.Length} bytes; the limit is {MAX_SECRET_SIZE}. " +
                              "This store is for credentials, not certificates, key files or blobs."
                };

            byte[] buffer = secret.ToArray();

            try
            {
                SecretOutcome outcome = await DoStoreAsync(key, buffer, token).ConfigureAwait(false);

                // A store can succeed (Found) or fail; NotFound from a store path is a
                // mis-mapped platform error that IsSuccess() would report as success.
                if (outcome.Status == SecretStatus.NotFound)
                    return new SecretOutcome
                    {
                        Status = SecretStatus.Failed,
                        Message = "The provider answered NotFound to a store — nothing was written. " +
                                  (outcome.Message ?? "")
                    };

                return outcome;
            }
            catch (Exception ex) when (MapException(ex) is { } mapped)
            {
                return new SecretOutcome { Status = mapped.Status, Message = mapped.Message };
            }
            finally
            {
                CryptographicOperations.ZeroMemory(buffer);
            }
        }

        /// <summary>
        /// Reads a secret after key validation. <see cref="SecretStatus.NotFound"/> is the
        /// only "not provisioned"; a store that will not open answers
        /// <see cref="SecretStatus.Unavailable"/>, never null. A provider answer of
        /// <see cref="SecretStatus.Found"/> with no bytes — a foreign or damaged entry — is
        /// normalized to <see cref="SecretStatus.Failed"/> here, so "" and absent can never
        /// be confused on the read side either.
        /// </summary>
        /// <param name="key">The secret's key — see <see cref="SecretKeys"/>.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The result; the secret bytes only when the status is <see cref="SecretStatus.Found"/>.</returns>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="ArgumentException">The key breaks the <see cref="SecretKeys"/> rules.</exception>
        public async Task<SecretResult> ReadAsync(string key, CancellationToken token = default)
        {
            SecretKeys.Validate(key);
            token.ThrowIfCancellationRequested();

            try
            {
                SecretResult result = await DoReadAsync(key, token).ConfigureAwait(false);

                if (result.Status == SecretStatus.Found &&
                    (result.Secret == null || result.Secret.Length == 0))
                    return new SecretResult
                    {
                        Status = SecretStatus.Failed,
                        Message = $"The '{Description.Key}' store answered Found with an empty " +
                                  "secret; the entry was not written by this library. " +
                                  (result.Message ?? "")
                    };

                return result;
            }
            catch (Exception ex) when (MapException(ex) is { } mapped)
            {
                return new SecretResult { Status = mapped.Status, Message = mapped.Message };
            }
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
        public async Task<SecretOutcome> DeleteAsync(string key, CancellationToken token = default)
        {
            SecretKeys.Validate(key);
            token.ThrowIfCancellationRequested();

            if (!Description.CanWrite)
                return DeniedReadOnly();

            try
            {
                return await DoDeleteAsync(key, token).ConfigureAwait(false);
            }
            catch (Exception ex) when (MapException(ex) is { } mapped)
            {
                return new SecretOutcome { Status = mapped.Status, Message = mapped.Message };
            }
        }

        /// <summary>
        /// Probes whether the store opens at all, so a host can gate at startup instead of
        /// discovering an absent Secret Service or vault at the first credential read,
        /// hours later. The default probes with a read of <see cref="PROBE_KEY"/>:
        /// <see cref="SecretStatus.NotFound"/> (or <see cref="SecretStatus.Found"/>) means
        /// the store opened and answered; anything else is the reason it will not, verbatim.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>An outcome whose <see cref="SecretOutcome.IsSuccess"/> says whether the
        /// store is reachable.</returns>
        public virtual async Task<SecretOutcome> CheckAsync(CancellationToken token = default)
        {
            SecretResult result = await ReadAsync(PROBE_KEY, token).ConfigureAwait(false);

            if (result.Status == SecretStatus.Found || result.Status == SecretStatus.NotFound)
                return new SecretOutcome { Status = SecretStatus.NotFound };

            return new SecretOutcome { Status = result.Status, Message = result.Message };
        }

        /// <summary>
        /// Provider store implementation. The key is valid, the secret is non-empty and
        /// inside the size limit, and the store is writable. <b>The buffer belongs to the
        /// base class and is zeroed as soon as this method returns</b> — a provider that
        /// must keep the bytes (an in-memory store) copies them.
        /// </summary>
        /// <param name="key">The validated key.</param>
        /// <param name="secret">The secret bytes; cleared by the base after the call.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The outcome.</returns>
        protected abstract Task<SecretOutcome> DoStoreAsync(string key, byte[] secret,
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

        #region Tools

        /// <summary>
        /// Classifies an exception a Do* method let escape into a status and a message a
        /// support engineer can act on, or null to treat it as a programming error and let
        /// it propagate. The base catches around every operation, so a provider states its
        /// platform's operational exceptions exactly once instead of wrapping every path.
        /// </summary>
        /// <param name="exception">The escaped exception.</param>
        /// <returns>The classification, or null to rethrow.</returns>
        protected virtual (SecretStatus Status, string Message)? MapException(Exception exception)
        {
            return null;
        }

        private SecretOutcome DeniedReadOnly()
        {
            return new SecretOutcome
            {
                Status = SecretStatus.Denied,
                Message = $"The '{Description.Key}' store is read-only."
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// What this store is and how well it protects.
        /// </summary>
        public abstract SecretStoreDescription Description { get; }

        #endregion
    }
}
