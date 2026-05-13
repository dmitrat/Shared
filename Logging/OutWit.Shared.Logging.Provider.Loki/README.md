# OutWit.Shared.Logging.Provider.Loki

Grafana Loki log provider plugin for OutWit hosts. Thin wrapper over
[`OutWit.Common.Logging.Loki`](https://www.nuget.org/packages/OutWit.Common.Logging.Loki)
that registers an [`ILogQueryProvider`](https://www.nuget.org/packages/OutWit.Common.Logging.Query)
which translates neutral `LogQuery` requests into LogQL and dispatches them
against `/loki/api/v1/*`.

## Configuration

Plugin reads its own `appsettings.json` from inside the deployed module folder:

```json
{
  "Loki": {
    "BaseUrl": "",
    "TenantId": "",
    "Username": "",
    "Password": "",
    "DefaultLabels": { "service_name": "WitIdentity" },
    "MaxResultLimit": 1000,
    "MaxRange": "7.00:00:00"
  }
}
```

| Setting | Default | Description |
|---|---|---|
| `BaseUrl` | _(required)_ | Loki HTTP endpoint, e.g. `http://loki:3100`. |
| `TenantId` | _(empty)_ | Multi-tenant Loki tenant id (`X-Scope-OrgID` header). Leave blank for single-tenant deployments. |
| `Username` | _(empty)_ | Basic-auth username (Grafana Cloud, nginx fronting Loki). |
| `Password` | _(empty)_ | Basic-auth password. **Should be supplied via env var `Loki__Password`** — keep the JSON value blank. |
| `DefaultLabels` | `{ service_name: WitIdentity }` | Stream selectors added to every query — narrow the scope to this host's logs. |
| `MaxResultLimit` | `1000` | Maximum entries per query (Loki rejects beyond its server-side cap). |
| `MaxRange` | `7.00:00:00` | Maximum time range per query (`d.hh:mm:ss`). Protects against accidental full-history scans. |

### Env-var override convention

```bash
Loki__BaseUrl=http://loki.internal:3100
Loki__TenantId=witidentity
Loki__Username=witidentity
Loki__Password=********                    # never put this in JSON
```

## Installation

```bash
dotnet add package OutWit.Shared.Logging.Provider.Loki
```

The plugin's `build/.targets` auto-copies the module to your output
`@Logging/loki.module/` at build time. The host's
`WitPluginLoader<ILogProviderPlugin>` discovers it.

## License

Apache 2.0 — see `LICENSE.txt`.
