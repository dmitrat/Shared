using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Shared.Secrets.Providers
{
    /// <summary>
    /// String convenience over <see cref="ISecretStore"/>. Convenience is fine; convenience
    /// that hides the trade-off is not — read the remarks on each method.
    /// </summary>
    public static class SecretStoreExtensions
    {
        #region Functions

        /// <summary>
        /// Stores a secret given as a string, encoded as UTF-8.
        /// </summary>
        /// <remarks>
        /// A .NET string is immutable, interned and moved by the collector, so <b>it cannot be
        /// reliably erased</b>: the secret's characters stay in this process's memory for as
        /// long as the collector pleases. By calling this the caller has accepted that. A caller
        /// who assembled the secret in a buffer should use
        /// <see cref="ISecretStore.StoreAsync"/> and clear the buffer instead. The intermediate
        /// encoding buffer this method creates is cleared before it returns.
        /// </remarks>
        /// <param name="store">The store.</param>
        /// <param name="key">The secret's key — see <see cref="SecretKeys"/>.</param>
        /// <param name="secret">The secret text.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The outcome; <see cref="SecretStatus.Found"/> on success.</returns>
        /// <exception cref="ArgumentNullException">The store, key or secret is null.</exception>
        /// <exception cref="ArgumentException">The key breaks the <see cref="SecretKeys"/> rules.</exception>
        public static async Task<SecretOutcome> StoreStringAsync(this ISecretStore store, string key,
            string secret, CancellationToken token = default)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            if (secret == null)
                throw new ArgumentNullException(nameof(secret));

            byte[] buffer = Encoding.UTF8.GetBytes(secret);

            try
            {
                return await store.StoreAsync(key, buffer, token).ConfigureAwait(false);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(buffer);
            }
        }

        /// <summary>
        /// Decodes a found secret as a UTF-8 string.
        /// </summary>
        /// <remarks>
        /// The resulting string <b>cannot be erased</b> — see the remarks on
        /// <see cref="StoreStringAsync"/>. The caller has accepted that.
        /// </remarks>
        /// <param name="result">A read result.</param>
        /// <returns>The secret as a string when the status is <see cref="SecretStatus.Found"/>;
        /// null otherwise.</returns>
        /// <exception cref="ArgumentNullException">The result is null.</exception>
        public static string? GetString(this SecretResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (result.Status != SecretStatus.Found || result.Secret == null)
                return null;

            return Encoding.UTF8.GetString(result.Secret);
        }

        #endregion
    }
}
