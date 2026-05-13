using OutWit.Common.Plugins.Abstractions.Interfaces;

namespace OutWit.Shared.Logging.Providers
{
    /// <summary>
    /// Plugin contract for a log query provider — a thin marker over <see cref="IWitPlugin"/>
    /// that lets a host scan a plugin folder for log backends without coupling to any
    /// vendor SDK. The selected plugin's <see cref="IWitPlugin.Initialize"/> registers
    /// an <see cref="OutWit.Common.Logging.Query.ILogQueryProvider"/> in DI; the host
    /// (admin UI, alerts pipeline, etc.) queries logs through the neutral provider.
    /// </summary>
    /// <remarks>
    /// The <see cref="Key"/> property distinguishes providers when more than one is
    /// dropped into the same <c>@Logging/</c> folder. An operator selects the active
    /// provider via configuration (e.g. <c>Logging:ProviderKey=NewRelic</c>) — each
    /// plugin inspects this value in its <c>Initialize</c> and only registers its
    /// transport when its <see cref="Key"/> matches.
    /// </remarks>
    public interface ILogProviderPlugin : IWitPlugin
    {
        /// <summary>
        /// Discriminator selected by an operator, e.g. <c>"NewRelic"</c>, <c>"Loki"</c>,
        /// <c>"File"</c>. Case-insensitive by convention.
        /// </summary>
        string Key { get; }
    }
}
