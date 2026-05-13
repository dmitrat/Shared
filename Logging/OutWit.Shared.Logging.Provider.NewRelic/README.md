# OutWit.Shared.Logging.Provider.NewRelic

NewRelic NerdGraph log provider plugin for OutWit hosts. Thin wrapper over
[`OutWit.Common.Logging.NewRelic`](https://www.nuget.org/packages/OutWit.Common.Logging.NewRelic)
that registers an [`ILogQueryProvider`](https://www.nuget.org/packages/OutWit.Common.Logging.Query)
which translates neutral `LogQuery` requests into NRQL and dispatches them
through NerdGraph.

The plugin additionally registers the NR-specific
`INewRelicProvider` superset for consumers that want the billing-style
`GetDataConsumptionAsync` — they can resolve either interface from DI
(both point at the same singleton).

## Configuration

Plugin reads its own `appsettings.json` from inside the deployed module folder:

```json
{
  "NewRelic": {
    "ApiKey": "",
    "AccountId": 0,
    "Endpoint": "https://api.newrelic.com/graphql",
    "DefaultPageSize": 100,
    "MaxPageSize": 1000
  }
}
```

| Setting | Default | Description |
|---|---|---|
| `ApiKey` | _(required)_ | User API key for NerdGraph. **Should be supplied via env var `NewRelic__ApiKey`** — keep the JSON value blank. |
| `AccountId` | _(required)_ | NewRelic account id (integer). Supply via `NewRelic__AccountId`. |
| `Endpoint` | `https://api.newrelic.com/graphql` | GraphQL endpoint. Override for EU region (`https://api.eu.newrelic.com/graphql`). |
| `DefaultPageSize` | `100` | Page size when the caller does not specify one. |
| `MaxPageSize` | `1000` | Upper bound enforced server-side; queries with larger page sizes are clamped. |

### Env-var override convention

```bash
NewRelic__ApiKey=NRAK-xxxxxxxxxxxxxxxxxx        # never put this in JSON
NewRelic__AccountId=1234567
```

## Installation

```bash
dotnet add package OutWit.Shared.Logging.Provider.NewRelic
```

The plugin's `build/.targets` auto-copies the module to your output
`@Logging/newrelic.module/` at build time. The host's
`WitPluginLoader<ILogProviderPlugin>` discovers it.

## License

Apache 2.0 — see `LICENSE.txt`.
