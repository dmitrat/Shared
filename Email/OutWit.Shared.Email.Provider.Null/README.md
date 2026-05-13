# OutWit.Shared.Email.Provider.Null

Zero-configuration **fallback** email plugin for OutWit hosts. Lets a host start with a working email pipeline before the operator has signed up for any real provider account. Implements [`IEmailTransport`](https://github.com/dmitrat/Common/tree/main/Email/OutWit.Common.Email) via [`IEmailProviderPlugin`](../OutWit.Shared.Email.Providers/).

## Modes

| Mode | Behavior | Use case |
|---|---|---|
| `LogOnly` _(default)_ | Logs `To`, `Subject` and the first line of the body at `Warning` level, then returns success. The operator can copy a password-reset / verify link straight from the logs. | Dev, staging, first-deploy walkthrough. |
| `Drop` | Logs an error and returns `EmailFailureKind.Permanent`. Flows that depend on email surface the failure cleanly via the host's `EmailFailureTracker` / health check. | Production deployments that genuinely don't need outbound email (e.g. corporate SSO-only WitIdentity). |

## Configuration

The plugin reads its own `appsettings.json` inside its module folder:

```json
{
  "Null": {
    "Mode": "LogOnly"
  }
}
```

Override via env var:

```bash
Null__Mode=Drop
```

## Installation

In the host:

```bash
dotnet add package OutWit.Shared.Email.Provider.Null
```

…then in the host's `Startup`/`Program.cs`:

```csharp
var loader = new WitPluginLoader<IEmailProviderPlugin>(
    Path.Combine(AppContext.BaseDirectory, "@Plugins"));
loader.Load();

foreach (var plugin in loader.Plugins)
    plugin.Initialize(services);
```

The plugin's package brings a `build/.targets` file that auto-copies `null.module/` into your output at build/publish time. The loader picks it up at runtime.

## License

Apache 2.0 — see `LICENSE`.
