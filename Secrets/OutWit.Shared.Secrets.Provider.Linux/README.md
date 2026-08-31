# OutWit.Shared.Secrets.Provider.Linux

Linux Secret Service (libsecret) provider for
[`OutWit.Shared.Secrets.Providers`](https://www.nuget.org/packages/OutWit.Shared.Secrets.Providers),
for interactive desktop sessions with an unlocked keyring.

## The limitation, stated prominently

**The Secret Service is a session service.** A systemd unit with no session, a
container, an SSH login without a desktop — these have no session bus and no
unlocked keyring, and every operation honestly answers
`SecretStatus.Unavailable`, never a silent fallback to a file. For those
deployments [`OutWit.Shared.Secrets.Provider.File`](https://www.nuget.org/packages/OutWit.Shared.Secrets.Provider.File)
is the intended answer, chosen deliberately in configuration.

## Where the secret lives

The session keyring, schema `com.outwit.secrets`, one item per key with the
attribute `key='{your key}'`. Attribute matching is exact and case-sensitive,
so the key maps identically — no suffix. The libsecret password API is
string-based, so **the payload is stored base64-encoded**; a support engineer
inspects it with:

```bash
secret-tool lookup key 'MyProduct/ApiKey'
```

One stated trade-off: the base64 string transits managed memory on the way in
and cannot be erased — the same trade-off the string extensions document.

## Use

```csharp
services.AddSingleton<ISecretStore>(new SecretStoreLibsecret());
// or, for a cross-platform desktop app: OutWit.Shared.Secrets.Desktop
```

Requires `libsecret-1.so.0` (package `libsecret-1-0` on Debian/Ubuntu,
`libsecret` on Fedora/Arch). Without it every operation answers `Unavailable`
with a message saying exactly that.
