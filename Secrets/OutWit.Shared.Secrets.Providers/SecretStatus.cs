namespace OutWit.Shared.Secrets.Providers
{
    /// <summary>
    /// The answer a secret store gives. Distinguishes "the store works and holds no secret"
    /// from "the store will not open" — conflating the two turns "the credential is there and
    /// I cannot open the box" into "this agent is not provisioned", which is an unfixable
    /// support call.
    /// </summary>
    public enum SecretStatus : byte
    {
        /// <summary>
        /// Absent, corrupt, or from a newer build. A default-constructed or badly deserialized
        /// result is never mistaken for a successful read or for a clean "not provisioned".
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The secret is here. For a store or delete outcome: the operation succeeded and this
        /// is the state the store is now in — a successful store reports <see cref="Found"/>.
        /// </summary>
        Found = 1,

        /// <summary>
        /// The store works and holds no secret under that key. This is the only
        /// "not provisioned". A successful delete reports <see cref="NotFound"/> — the state
        /// the store is now in — whether or not the secret existed before.
        /// </summary>
        NotFound = 2,

        /// <summary>
        /// The store exists but will not open — a locked keyring, no session bus,
        /// no vault for this account.
        /// </summary>
        Unavailable = 3,

        /// <summary>
        /// The store opened and refused this caller.
        /// </summary>
        Denied = 4,

        /// <summary>
        /// The store failed in a way it could not classify.
        /// </summary>
        Failed = 5
    }
}
