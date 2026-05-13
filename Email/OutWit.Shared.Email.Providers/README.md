# OutWit.Shared.Email.Providers

Plugin contract for OutWit email providers. One file, one interface — `IEmailProviderPlugin`, a marker over [`IWitPlugin`](https://github.com/dmitrat/Common/tree/main/Plugins/OutWit.Common.Plugins.Abstractions) that lets a host scan a plugin folder for vendor-specific transports (Resend, SMTP, AWS SES, …) without coupling to any one SDK.

> **Plugin authors**: see the [OutWit Plugin Architecture Guide](https://github.com/dmitrat/Common/blob/main/Plugins/PLUGINS_GUIDE.md) for the end-to-end workflow (csproj setup, nuspec packaging, `build/*.targets`, module-folder layout). This package contains only the contract.

## Plugin author quickstart

```csharp
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Email;                          // IEmailTransport, EmailMessage, ...
using OutWit.Common.Plugins.Abstractions;           // WitPluginBase
using OutWit.Common.Plugins.Abstractions.Attributes; // [WitPluginManifest]
using OutWit.Shared.Email.Providers;                  // IEmailProviderPlugin

[WitPluginManifest("Mailgun Email Provider", Version = "1.0.0")]
public sealed class MailgunEmailProviderPlugin : WitPluginBase, IEmailProviderPlugin
{
    public string Key => "Mailgun";

    public override void Initialize(IServiceCollection services)
    {
        services.AddSingleton<IEmailTransport, MailgunEmailTransport>();
    }
}
```

## Host author quickstart

```csharp
using OutWit.Common.Plugins;
using OutWit.Shared.Email.Providers;

var loader = new WitPluginLoader<IEmailProviderPlugin>(
    Path.Combine(AppContext.BaseDirectory, "@Email"));
loader.Load();

foreach (var plugin in loader.Plugins)
    plugin.Initialize(services);

var sp = services.BuildServiceProvider();

foreach (var plugin in loader.Plugins)
    plugin.OnInitialized(sp);

var transport = sp.GetRequiredService<IEmailTransport>();
await transport.SendAsync(message);
```

## Bundled / reference plugins

| Package | Key | Notes |
|---|---|---|
| [`OutWit.Shared.Email.Provider.Null`](../OutWit.Shared.Email.Provider.Null/) | `Null` | Zero-config fallback. Two modes: `LogOnly` (prints message to logs — useful in dev) and `Drop` (fails fast — useful in production-without-email). |
| [`OutWit.Shared.Email.Plugin.Resend`](../OutWit.Shared.Email.Plugin.Resend/) | `Resend` | HTTP API-based, modern transactional email. |
| `OutWit.Shared.Email.Plugin.Smtp` _(planned)_ | `Smtp` | MailKit-based, covers on-prem / corporate Exchange / dev SMTP catchers like MailHog. |

## License

Apache 2.0 — see `LICENSE`.
