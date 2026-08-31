# OutWit.Shared.Secrets.Providers

Secret-storage abstractions for OutWit hosts: `ISecretStore` over the operating
system's credential store, for the one or two long-lived credentials a product
holds — tokens, keys, passphrases. Not certificates, key files or blobs: secrets
above 1024 bytes are refused with a named failure.

This is the **contract package** — install a provider to get an actual store:

- [`OutWit.Shared.Secrets.Provider.Windows`](https://www.nuget.org/packages/OutWit.Shared.Secrets.Provider.Windows) — Windows Credential Manager
- [`OutWit.Shared.Secrets.Provider.File`](https://www.nuget.org/packages/OutWit.Shared.Secrets.Provider.File) — the explicitly-labelled file fallback
- (planned) `OutWit.Shared.Secrets.Provider.Linux` (libsecret), `OutWit.Shared.Secrets.Provider.MacOS` (Keychain)

## The status model — the whole point

A store that answers `null` for "no such secret", for "the keyring is locked",
and for "there is no D-Bus session here" produces a product that reports *"this
agent is not provisioned"* when the truth is *"the credential is there and I
cannot open the box"*. That is an unfixable support call, and it is the default
outcome of the obvious API. So nothing here returns null and nothing throws for
an expected condition:

| `SecretStatus` | Meaning |
|---|---|
| `Found` | The secret is here (and: a store succeeded) |
| `NotFound` | The store works and holds no such secret — the **only** "not provisioned" (and: a delete succeeded) |
| `Unavailable` | The store will not open — locked keyring, no session bus, no vault for this account |
| `Denied` | The store opened and refused this caller |
| `Failed` | Something else, named in `Message` |
| `Unknown` | Default value; never a valid answer |

Exceptions are reserved for programming errors — a null key, a key that breaks
the rules below.

## Use

```csharp
ISecretStore store = /* register a provider at the composition root */;

// Log once at startup what you actually got:
logger.LogInformation("Secret store: {Description}", store.Description);

var result = await store.ReadAsync("MyProduct/ApiKey");
switch (result.Status)
{
    case SecretStatus.Found:      /* use result.Secret, clear it when done */ break;
    case SecretStatus.NotFound:   /* genuinely not provisioned */            break;
    default:                      /* do NOT treat as "not provisioned"! */   break;
}
```

Keys are `{Product}/{Purpose}` — case-sensitive ASCII `[A-Za-z0-9._/-]`, 1–128
characters. Secrets are `byte[]`, not `string`: a .NET string cannot be reliably
erased. `StoreStringAsync` / `GetString` extensions exist for convenience and
say what they cost.

## What this defends against — and what it does not

It defends against: another user of the same machine reading the credential; a
credential sitting in a settings file, and therefore in a support bundle, a
backup, a screenshot, or a version-control checkout; casual disclosure through
ordinary tooling.

It does **not** defend against: an administrator on the machine; an attacker
running as the owning account; an offline attack on a stolen disk or image;
cloning. It raises reading a credential from *"open the config file"* to
*"obtain administrative control of the machine or its disk"* — design everything
downstream of the credential on the assumption that the second is possible:
keep stored credentials centrally revocable.

## Uninstall

An uninstaller must `DeleteAsync` the keys it provisioned. There is nothing the
library can do to enforce that, which is why it is written here: a credential
that survives uninstallation sits in the vault indefinitely, and is still valid.
