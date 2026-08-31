using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Values;

namespace OutWit.Shared.Secrets.Providers
{
    /// <summary>
    /// What a store is and how well it protects. A host logs this once at startup and can
    /// refuse to run on a protection level a configuration does not accept.
    /// </summary>
    public sealed class SecretStoreDescription : ModelBase
    {
        #region Functions

        /// <summary>
        /// Value comparison.
        /// </summary>
        /// <param name="modelBase">The model to compare with.</param>
        /// <param name="tolerance">Unused for this model.</param>
        /// <returns>True when all fields match.</returns>
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not SecretStoreDescription other)
                return false;

            return Key.Is(other.Key)
                   && Protection == other.Protection
                   && CanWrite == other.CanWrite
                   && Location.Is(other.Location);
        }

        /// <summary>
        /// Creates a copy.
        /// </summary>
        /// <returns>A new <see cref="SecretStoreDescription"/> with the same values.</returns>
        public override ModelBase Clone()
        {
            return new SecretStoreDescription
            {
                Key = Key,
                Protection = Protection,
                CanWrite = CanWrite,
                Location = Location
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Provider key, e.g. "Windows", "Libsecret", "Keychain", "File".
        /// </summary>
        [ToString]
        public string Key { get; init; } = "";

        /// <summary>
        /// What is actually protecting the secret — see <see cref="SecretProtection"/>.
        /// </summary>
        [ToString]
        public SecretProtection Protection { get; init; }

        /// <summary>
        /// False for a read-only provider — environment variables, mounted container secrets,
        /// a cloud secret manager consumed but not managed here. The conformance suite runs
        /// only the read-side tests for such a provider, and
        /// <see cref="SecretStoreBase"/> answers <see cref="SecretStatus.Denied"/> to writes.
        /// </summary>
        [ToString]
        public bool CanWrite { get; init; } = true;

        /// <summary>
        /// Where a support engineer will find the entry. Never a secret.
        /// </summary>
        [ToString]
        public string Location { get; init; } = "";

        #endregion
    }
}
