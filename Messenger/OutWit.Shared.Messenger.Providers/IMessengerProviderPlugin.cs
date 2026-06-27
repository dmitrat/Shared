using OutWit.Common.Plugins.Abstractions.Interfaces;

namespace OutWit.Shared.Messenger.Providers
{
    /// <summary>
    /// Plugin contract for a messenger provider — a thin marker over <see cref="IWitPlugin"/>
    /// that lets a host scan a plugin folder for transports without coupling to any
    /// vendor SDK. The selected plugin's <see cref="IWitPlugin.Initialize"/> registers
    /// an <see cref="OutWit.Common.Messenger.IMessengerTransport"/> in DI; the host then
    /// sends messages through the neutral transport interface.
    /// </summary>
    /// <remarks>
    /// The <see cref="Key"/> property distinguishes providers when more than one is
    /// dropped into the same <c>@Messenger/</c> folder. An operator selects the active
    /// provider via configuration (e.g. <c>Messenger:ProviderKey=Telegram</c>) — each
    /// plugin inspects this value in its <c>Initialize</c> and only registers its
    /// transport when its <see cref="Key"/> matches.
    /// </remarks>
    public interface IMessengerProviderPlugin : IWitPlugin
    {
        /// <summary>
        /// Discriminator selected by an operator, e.g. <c>"Telegram"</c>, <c>"Slack"</c>,
        /// <c>"Null"</c>. Case-insensitive by convention.
        /// </summary>
        string Key { get; }
    }
}
