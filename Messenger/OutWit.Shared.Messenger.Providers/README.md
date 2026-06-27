# OutWit.Shared.Messenger.Providers

Plugin contract for OutWit messenger providers — the counterpart of
`OutWit.Shared.Email.Providers` for chat/IM notifications.

Defines **`IMessengerProviderPlugin`** (a marker over `IWitPlugin`) so a host can
load Telegram / Slack / etc. transports via `WitPluginLoader` from a `@Messenger`
folder, without compile-time coupling to any vendor SDK.

The selected plugin's `Initialize` registers an
`OutWit.Common.Messenger.IMessengerTransport` in DI. An operator picks the active
provider with `Messenger:ProviderKey` (e.g. `Messenger__ProviderKey=Telegram`);
each plugin registers its transport only when its `Key` matches.

Concrete providers: `OutWit.Shared.Messenger.Provider.Telegram`,
`OutWit.Shared.Messenger.Provider.Null`.
