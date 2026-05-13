using OutWit.Common.Plugins.Abstractions.Interfaces;

namespace OutWit.Shared.Email.Plugins
{
    /// <summary>
    /// Plugin contract for an email provider — a thin marker over <see cref="IWitPlugin"/>
    /// that lets a host scan a plugin folder for transports without coupling to any
    /// vendor SDK. The selected plugin's <see cref="IWitPlugin.Initialize"/> registers
    /// an <see cref="OutWit.Common.Email.IEmailTransport"/> in DI; the host then sends
    /// mail through the neutral transport interface.
    /// </summary>
    /// <remarks>
    /// The <see cref="Key"/> property distinguishes providers when more than one is
    /// dropped into the same <c>@Plugins/</c> folder. An operator selects the active
    /// provider via configuration (e.g. <c>Email:ProviderKey=Resend</c>) — each plugin
    /// inspects this value in its <c>Initialize</c> and only registers its transport
    /// when its <see cref="Key"/> matches.
    /// </remarks>
    public interface IEmailProviderPlugin : IWitPlugin
    {
        /// <summary>
        /// Discriminator selected by an operator, e.g. <c>"Resend"</c>, <c>"Smtp"</c>,
        /// <c>"AwsSes"</c>, <c>"Null"</c>. Case-insensitive by convention.
        /// </summary>
        string Key { get; }
    }
}
