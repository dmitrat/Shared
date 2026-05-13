# OutWit.Shared.Email.Provider.Smtp

SMTP email transport plugin for OutWit hosts. Implements [`IEmailTransport`](https://www.nuget.org/packages/OutWit.Common.Email) over [MailKit](https://github.com/jstedfast/MailKit) — covers on-prem relays, corporate Exchange, hosted SMTP services (SendGrid SMTP, Amazon SES SMTP, Mailgun SMTP), and local dev catchers like [MailHog](https://github.com/mailhog/MailHog) and [Papercut-SMTP](https://github.com/ChangemakerStudios/Papercut-SMTP).

## Configuration

Plugin reads its own `appsettings.json` from inside the deployed module folder:

```json
{
  "Smtp": {
    "Host": "",
    "Port": 587,
    "Username": "",
    "Password": "",
    "Security": "StartTls",
    "Timeout": "00:00:30",
    "AllowSelfSignedCertificates": false
  }
}
```

| Setting | Default | Description |
|---|---|---|
| `Host` | _(required)_ | SMTP server host name. |
| `Port` | `587` | TCP port. Typical values: 25 (relay/no-TLS), 465 (SslOnConnect), 587 (StartTls). |
| `Username` | _(empty)_ | Auth username. Leave blank for unauthenticated relays (LAN, dev SMTP catchers). |
| `Password` | _(empty)_ | Auth password. **Should be supplied via env var `Smtp__Password`** — keep the JSON value blank. |
| `Security` | `StartTls` | TLS mode: `None`, `StartTls`, `SslOnConnect`, `Auto`. |
| `Timeout` | `00:00:30` | Connection / send timeout (`hh:mm:ss`). |
| `AllowSelfSignedCertificates` | `false` | Accept untrusted server certs. **Dev only** — MailHog/Papercut use self-signed certs. Never in production. |

### Env-var override convention

Standard .NET configuration binding maps double-underscore env vars to JSON keys:

```bash
Smtp__Host=smtp.example.com
Smtp__Port=587
Smtp__Username=postmaster@example.com
Smtp__Password=********           # never put this in JSON
Smtp__Security=StartTls
```

## Installation

```bash
dotnet add package OutWit.Shared.Email.Provider.Smtp
```

The plugin's `build/.targets` auto-copies the module to your output `@Email/smtp.module/` at build time. The host's `WitPluginLoader<IEmailProviderPlugin>(Path.Combine(AppContext.BaseDirectory, "@Email"))` discovers it.

## Failure classification

MailKit exceptions are mapped to neutral `EmailFailureKind` values so the host's failure tracker / retry policy works the same regardless of provider:

| Exception / SMTP code | Kind |
|---|---|
| `AuthenticationException`, SMTP 535 | `AuthFailure` |
| SMTP 421, 450, 451, 452 | `Transient` (caller may retry) |
| SMTP 550, 551, 553 | `InvalidRecipient` (don't retry, mark recipient bad) |
| SMTP 552 (message too large) | `Permanent` |
| `IOException` / `SocketException` / `TimeoutException` | `Transient` |
| Any other 5xx, generic exceptions | `Permanent` |

## Testing locally with MailHog

```bash
docker run -d -p 1025:1025 -p 8025:8025 --name mailhog mailhog/mailhog
```

Plugin config (or env):

```bash
Smtp__Host=localhost
Smtp__Port=1025
Smtp__Security=None
```

UI at `http://localhost:8025` shows captured messages.

## License

Apache 2.0 — see `LICENSE.txt`.
