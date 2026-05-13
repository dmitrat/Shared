# OutWit.Shared.Logging.Providers

Plugin contract for OutWit log query providers. Defines
[`ILogProviderPlugin`](ILogProviderPlugin.cs) — a thin marker over
[`IWitPlugin`](https://www.nuget.org/packages/OutWit.Common.Plugins.Abstractions)
that lets a host scan a `@Logging/` folder for log-backend implementations
without coupling to any vendor SDK.

The selected plugin registers an
[`ILogQueryProvider`](https://www.nuget.org/packages/OutWit.Common.Logging.Query)
in DI; the host (admin UI, alerts pipeline, etc.) queries logs through the
neutral provider — same shape regardless of NewRelic / Loki / local files
underneath.

## Wiring (host side)

```csharp
var loader = new WitPluginLoader<ILogProviderPlugin>(
    Path.Combine(AppContext.BaseDirectory, "@Logging"),
    useIsolatedContexts: false);
loader.Load();

var active = loader.FirstOrDefault(p => p.Key.Equals("NewRelic", StringComparison.OrdinalIgnoreCase))
             ?? throw new InvalidOperationException("Logging:ProviderKey not found");

// Optionally publish host-known log path for file-scanning plugins:
services.AddSingleton(new HostLoggingInfo { LogsPath = "/app/logs", FilePattern = "log-*.json" });

active.Initialize(services);

// Now services has ILogQueryProvider — query through it:
var provider = services.BuildServiceProvider().GetRequiredService<ILogQueryProvider>();
```

## Wiring (plugin side)

```csharp
[WitPluginManifest("NewRelic Log Provider", Version = "1.0.0")]
public sealed class NewRelicLogProviderPlugin : WitPluginBase, ILogProviderPlugin
{
    public string Key => "NewRelic";

    public override void Initialize(IServiceCollection services)
    {
        var options = ReadOptions();
        services.AddSingleton(options);
        services.AddHttpClient<NewRelicHttpClient>();
        services.AddSingleton<ILogQueryProvider, NewRelicLogQueryProvider>();
    }
}
```

## License

Apache 2.0 — see `LICENSE.txt`.
