# OutWit.Shared.Secrets.Provider.Windows

Windows Credential Manager provider for
[`OutWit.Shared.Secrets.Providers`](https://www.nuget.org/packages/OutWit.Shared.Secrets.Providers):
generic credentials via `advapi32` (`LibraryImport` source generation —
NativeAOT-clean), `CRED_PERSIST_LOCAL_MACHINE`.

## Where the secret actually lives

**In the vault of the account the process runs as.** `CRED_PERSIST_LOCAL_MACHINE`
does not mean "any user on this machine" — it means "this user, on this machine,
and do not roam". That is the containment: a service's credential is invisible
to every interactive user, and an interactive user's credential to every other
account. It also means the service account's entries do **not** appear in an
administrator's Credential Manager UI.

A support engineer looks in: *Credential Manager → Windows Credentials →
Generic Credentials*, under the target name `{key}#{first 8 hex of SHA-256(key)}`.
The suffix exists because Windows target names are case-insensitive while keys
are case-sensitive; `SecretStoreWindows.MapKey(key)` computes it.

## Provisioning — read this before writing the installer

Whatever writes the credential at install time must do so **as the account that
will read it**. An installer running elevated writes into the administrator's
vault by default, and the resulting service cannot read its own credential; the
symptom — "registration is refused on this one machine" — is very hard to read
backwards. Impersonate the service account (or run as it), then **verify with a
read as that account** before reporting success.

For a *virtual service account* (`NT SERVICE\...`) note that its profile — and
therefore its vault — is created lazily on first use. Prove the vault works for
your account on a real service before committing to it.

## Use

```csharp
services.AddSingleton<ISecretStore>(new SecretStoreWindows());
```

Uninstallers must `DeleteAsync` the keys they provisioned — an entry left behind
outlives the product and is still valid.
