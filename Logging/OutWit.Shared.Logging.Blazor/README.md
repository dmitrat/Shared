# OutWit.Shared.Logging.Blazor

Blazor WebAssembly log viewer for OutWit admin UIs. Provides a complete set
of MudBlazor-based components for querying, filtering, and displaying logs
from any [`ILogQueryProvider`](https://www.nuget.org/packages/OutWit.Common.Logging.Query) — works equally well over
NewRelic NerdGraph, Grafana Loki, or local Serilog JSON files.

The component layer is **vendor-neutral**: the host wires up the active
log provider via `OutWit.Shared.Logging.Provider.*` plugins (loaded from
the host's `@Logging/` folder, selected by `Logging:ProviderKey`), and the
UI talks to `ILogQueryProvider` exclusively.

## Components

| Component | Role |
|---|---|
| `LogsToolbar` | Date / time range, severity, source dropdowns, paging |
| `LogsFilterTree` | Hierarchical, savable filter chains with multi-level highlight |
| `LogsTable` | Virtualized table with level-coloured rows |
| `LogsDetails` | Expanded view of a single entry: message + attributes |
| `DialogEditLogFilter` | Modal editor for a filter node |
| `DialogLogsStatistics` | Counts-by-level / storage / billing breakdown |

## Static assets

Razor class libraries serve static assets under `_content/{AssemblyName}/`.
For this package add the stylesheet to the host's `index.html`:

```html
<link href="_content/OutWit.Shared.Logging.Blazor/css/m3-logs.css" rel="stylesheet" />
```

## Installation

```bash
dotnet add package OutWit.Shared.Logging.Blazor
```

The component layer is decoupled from the provider plugins — install only
the plugin packages your deployment needs. The base contract package
[`OutWit.Common.Logging.Query`](https://www.nuget.org/packages/OutWit.Common.Logging.Query)
is pulled in transitively.

## License

Apache 2.0 — see `LICENSE.txt`.
