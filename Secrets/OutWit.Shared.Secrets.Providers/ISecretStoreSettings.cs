namespace OutWit.Shared.Secrets.Providers
{
    /// <summary>
    /// Configuration settings for the secret storage subsystem. Providers are registered by
    /// the composition root — there is no plugin loader: a secret store is chosen per platform,
    /// which the build already knows. <see cref="ProviderKey"/> remains the mechanism for
    /// services and for explicitly choosing the File provider.
    /// </summary>
    public interface ISecretStoreSettings
    {
        /// <summary>
        /// Provider key, matching a registered provider's
        /// <see cref="SecretStoreDescription.Key"/>. No default: the host chooses.
        /// </summary>
        string ProviderKey { get; }
    }
}
