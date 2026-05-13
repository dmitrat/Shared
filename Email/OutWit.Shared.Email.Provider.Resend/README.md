# OutWit.Shared.Email.Provider.Resend

Resend email transport plugin for OutWit hosts. Implements [`IEmailTransport`](https://www.nuget.org/packages/OutWit.Common.Email) over the [Resend .NET SDK](https://github.com/resend/resend-dotnet) — HTTP API at `api.resend.com`. This is the default provider used in the WitIdentity release installation.

## Configuration

Plugin reads its own `appsettings.json` from inside the deployed module folder:

```json
{
  "Resend": {
    "ApiToken": "",
    "ApiUrl": "https://api.resend.com"
  }
}
```

| Setting | Default | Description |
|---|---|---|
| `ApiToken` | _(required)_ | Resend API key (starts with `re_`). **Should be supplied via env var `Resend__ApiToken`** — keep the JSON value blank. |
| `ApiUrl` | `https://api.resend.com` | API base URL. Override only to point at a staging endpoint or a self-hosted proxy. |

### Env-var override convention

Standard .NET configuration binding maps double-underscore env vars to JSON keys:

```bash
Resend__ApiToken=re_xxxxxxxxxxxxxxxxxxxxxxxx     # never put this in JSON
Resend__ApiUrl=https://api.resend.com
```

## Installation

```bash
dotnet add package OutWit.Shared.Email.Provider.Resend
```

The plugin's `build/.targets` auto-copies the module to your output `@Email/resend.module/` at build time. The host's `WitPluginLoader<IEmailProviderPlugin>(Path.Combine(AppContext.BaseDirectory, "@Email"))` discovers it.

## Failure classification

Resend SDK exceptions are mapped to neutral `EmailFailureKind` values so the host's failure tracker / retry policy works the same regardless of provider:

| Resend error / HTTP code | Kind |
|---|---|
| `MissingApiKey`, `InvalidApiKey`, `RestrictedApiKey`, `InvalidAccess`, HTTP 401/403 | `AuthFailure` |
| `RateLimitExceeded`, `MonthlyQuotaExceeded`, `DailyQuotaExceeded`, HTTP 429 | `RateLimited` (retry with backoff) |
| `InternalServerError`, `ApplicationError`, `ConcurrentIdempotentRequests`, HTTP 5xx | `Transient` (caller may retry) |
| `HttpRequestException`, `TaskCanceledException`, `TimeoutException` | `Transient` |
| `ValidationError`, `InvalidFromAddress`, `MissingRequiredField`, HTTP 422 | `Permanent` |
| Any other exception | `Permanent` |

`InvalidRecipient` is not produced — Resend lumps bad addresses into the generic `ValidationError` bucket, so we surface them as `Permanent`.

## License

Apache 2.0 — see `LICENSE.txt`.
