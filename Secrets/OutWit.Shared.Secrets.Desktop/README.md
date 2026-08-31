# OutWit.Shared.Secrets.Desktop

The composite package for a cross-platform desktop application — one reference,
one line:

```csharp
ISecretStore store = SecretStoreDesktop.ForCurrentPlatform();
logger.LogInformation("Secret store: {Description}", store.Description);
```

It references all three OS providers and picks at runtime: Credential Manager
on Windows, the login Keychain on macOS, the Secret Service on Linux. The other
platforms' providers are small managed assemblies whose P/Invoke binds lazily —
they cost nothing on the platforms where they are never called, and none of
them bundles a native library: each talks to what the OS already has.

## Why runtime selection is fine here — and where it is not

Selecting **among the operating-system providers** is not silent degradation:
on each desktop platform exactly one OS store is right, and all three protect
the same way (`SecretProtection.OperatingSystem`). What stays forbidden is
falling back to a *file* automatically — that changes what protects the secret,
invisibly. So the factory throws `PlatformNotSupportedException` rather than
degrade; configuring
[`OutWit.Shared.Secrets.Provider.File`](https://www.nuget.org/packages/OutWit.Shared.Secrets.Provider.File)
is a deliberate, visible choice.

One Linux reality to plan for: a desktop without a working keyring (headless,
exotic WM) answers `Unavailable` to everything. Decide at the product level
what the application does then — re-authenticate each start, or an explicit,
consented switch to the File provider — and log `Description` either way.
