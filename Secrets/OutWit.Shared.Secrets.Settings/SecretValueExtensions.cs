using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Settings.Values;
using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Settings
{
    /// <summary>
    /// The store glue for <see cref="SecretValue"/>. The settings side knows only the
    /// reference — store key, set flag, hint; these extensions reach the actual secret in
    /// an <see cref="ISecretStore"/> at the point of use. Mutating operations return an
    /// <b>updated copy</b> rather than mutating in place: assign it back to the settings
    /// property so the settings pipeline sees the change and persists the new reference.
    /// </summary>
    public static class SecretValueExtensions
    {
        #region Functions

        /// <summary>
        /// Reads the secret this value refers to. The full status model is preserved —
        /// <see cref="SecretStatus.NotFound"/> is the only "not provisioned", and a store
        /// that will not open says so; decode a found secret with
        /// <see cref="SecretStoreExtensions.GetString"/>.
        /// </summary>
        /// <param name="value">The secret reference.</param>
        /// <param name="store">The secret store.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The read result.</returns>
        /// <exception cref="ArgumentNullException">The value or store is null.</exception>
        /// <exception cref="ArgumentException">The reference carries no store key, or the
        /// key breaks the <see cref="SecretKeys"/> rules.</exception>
        public static Task<SecretResult> RevealAsync(this SecretValue value, ISecretStore store,
            CancellationToken token = default)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (store == null)
                throw new ArgumentNullException(nameof(store));

            return store.ReadAsync(value.StoreKey, token);
        }

        /// <summary>
        /// Stores a new secret under this value's key and returns the updated reference —
        /// set flag on, hint recomputed. On failure the original reference is returned
        /// unchanged, so a straight assignment back to the settings property is always safe.
        /// </summary>
        /// <remarks>
        /// The secret is a string and cannot be erased from managed memory — the stated
        /// trade-off of the string path, accepted here because a settings UI hands values
        /// around as strings anyway.
        /// </remarks>
        /// <param name="value">The secret reference.</param>
        /// <param name="store">The secret store.</param>
        /// <param name="secret">The new secret.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The outcome, and the reference to assign back.</returns>
        /// <exception cref="ArgumentNullException">The value, store or secret is null.</exception>
        /// <exception cref="ArgumentException">The reference carries no store key, or the
        /// key breaks the <see cref="SecretKeys"/> rules.</exception>
        public static async Task<(SecretOutcome Outcome, SecretValue Value)> SetAsync(
            this SecretValue value, ISecretStore store, string secret,
            CancellationToken token = default)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (store == null)
                throw new ArgumentNullException(nameof(store));

            SecretOutcome outcome = await store.StoreStringAsync(value.StoreKey, secret, token)
                .ConfigureAwait(false);

            if (!outcome.IsSuccess())
                return (outcome, value);

            return (outcome, new SecretValue
            {
                StoreKey = value.StoreKey,
                IsSet = true,
                Hint = SecretValue.MakeHint(secret)
            });
        }

        /// <summary>
        /// Removes the secret this value refers to and returns the updated reference — set
        /// flag off, hint gone. Removing one that is not there succeeds.
        /// </summary>
        /// <param name="value">The secret reference.</param>
        /// <param name="store">The secret store.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The outcome, and the reference to assign back.</returns>
        /// <exception cref="ArgumentNullException">The value or store is null.</exception>
        /// <exception cref="ArgumentException">The reference carries no store key, or the
        /// key breaks the <see cref="SecretKeys"/> rules.</exception>
        public static async Task<(SecretOutcome Outcome, SecretValue Value)> ClearAsync(
            this SecretValue value, ISecretStore store, CancellationToken token = default)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (store == null)
                throw new ArgumentNullException(nameof(store));

            SecretOutcome outcome = await store.DeleteAsync(value.StoreKey, token)
                .ConfigureAwait(false);

            if (!outcome.IsSuccess())
                return (outcome, value);

            return (outcome, new SecretValue
            {
                StoreKey = value.StoreKey,
                IsSet = false,
                Hint = ""
            });
        }

        #endregion
    }
}
