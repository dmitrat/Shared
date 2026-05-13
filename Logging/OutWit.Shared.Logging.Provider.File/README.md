# OutWit.Shared.Logging.Provider.File

File-scanning log query plugin for OutWit hosts — the "no external account
needed" fallback. Reads the host's Serilog NDJSON files in-place and exposes
them through the neutral
[`ILogQueryProvider`](https://www.nuget.org/packages/OutWit.Common.Logging.Query)
interface — the same shape NewRelic and Loki providers expose.

## Where the logs come from

The plugin reads files from the directory the **host** writes them to. The
host registers an [`OutWit.Shared.Logging.Providers.HostLoggingInfo`](https://www.nuget.org/packages/OutWit.Shared.Logging.Providers)
singleton in DI before invoking the plugin's `Initialize`, and the plugin
picks it up at first query. This avoids duplicating the Serilog path into
two places.

```csharp
// Host wiring (e.g. WitIdentity Startup):
services.AddSingleton(new HostLoggingInfo
{
    LogsPath    = "/app/logs",
    FilePattern = "log-*.json"
});
plugin.Initialize(services);   // ← FileLogProviderPlugin
```

When the host did not register a `HostLoggingInfo`, the plugin falls back
to its own `File:FallbackLogsPath` / `File:FallbackFilePattern` settings.

## Configuration

Plugin reads its own `appsettings.json` from inside the deployed module folder:

```json
{
  "File": {
    "MaxScannedEntries": 50000,
    "FallbackLogsPath": "logs",
    "FallbackFilePattern": "log-*.json"
  }
}
```

| Setting | Default | Description |
|---|---|---|
| `MaxScannedEntries` | `50000` | Maximum number of lines parsed per query. Caps the work each request can do regardless of how much history sits on disk. |
| `FallbackLogsPath` | `logs` | Directory used only when the host did not register a `HostLoggingInfo`. Relative paths resolve under the plugin's base directory. |
| `FallbackFilePattern` | `log-*.json` | Glob pattern used only when the host did not register a `HostLoggingInfo`. |

Standard env-var override convention applies (`File__MaxScannedEntries=200000`).

## Supported JSON formats

The parser is defensive about field names — supports both the NewRelic
log enricher format (the WitIdentity production default —
`timestamp` / `log.level` / `message` / `service.name` / `hostname` …)
and Serilog's compact JSON shape (`@t` / `@l` / `@mt` / `@x`). Mixing files
of different shapes in the same directory works.

## Honest limitations

| Limitation | Why | Workaround |
|---|---|---|
| Only sees this host's logs | Files live on local disk | For multi-replica, mount a shared volume or switch to NewRelic / Loki |
| O(scan) per query | Full file scan filtered in-memory | `MaxScannedEntries` caps work per request; cap your time range |
| Loses old logs on rotation | Serilog `retainedFileCountLimit` is the upper bound | Increase the limit or ship to a log service |
| No live tail | Files are scanned, not subscribed | Refresh the page; latency = polling interval |

Practical for: single-instance deployments, air-gapped installs,
development, disaster fallback if NerdGraph / Loki is unreachable. Not
suitable for distributed / high-volume deployments — use a real log
backend there.

## Installation

```bash
dotnet add package OutWit.Shared.Logging.Provider.File
```

The plugin's `build/.targets` auto-copies the module to your output
`@Logging/file.module/` at build time. The host's
`WitPluginLoader<ILogProviderPlugin>` discovers it.

## License

Apache 2.0 — see `LICENSE.txt`.
