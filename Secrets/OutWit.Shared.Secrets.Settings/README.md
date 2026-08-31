# OutWit.Shared.Secrets.Settings

The bridge between `OutWit.Common.Settings`' `SecretValue` and
`OutWit.Shared.Secrets`' `ISecretStore`. The settings side keeps only a
reference — store key, set flag, and a non-secret hint; this package reaches
the actual secret at the point of use. The secret never touches the settings
file, the settings pipeline, or a backup of either.

## Declare

In the settings template, the entry is a reference from day one:

```json
{ "key": "ApiKey", "valueKind": "Secret", "value": "MyProduct/ApiKey||" }
```

The settings property is a `SecretValue` — `StoreKey`, `IsSet`, `Hint` — which
is everything a UI needs: show `wit_sk_••••{Hint}` when `IsSet`, offer
Replace/Clear, and disable the editor with a plain-language message when the
store's `CheckAsync` says it will not open.

## Use

```csharp
// Read, where the secret is actually needed:
var result = await settings.ApiKey.RevealAsync(store);
if (result.Status == SecretStatus.Found)
    client.Authenticate(result.GetString()!);

// Write, from the settings UI — assign the updated reference back so the
// settings pipeline sees the change:
(var outcome, settings.ApiKey) = await settings.ApiKey.SetAsync(store, enteredKey);

// Clear:
(outcome, settings.ApiKey) = await settings.ApiKey.ClearAsync(store);
```

## Migrating a plaintext "Password" setting

One-time, at startup, silently:

```csharp
if (!string.IsNullOrEmpty(settings.LegacyApiKey))
{
    (var outcome, settings.ApiKey) = await settings.ApiKey.SetAsync(store, settings.LegacyApiKey);
    if (outcome.IsSuccess())
        settings.LegacyApiKey = "";   // overwrite, then save
}
```

A secret that has lived in a plaintext settings file is a secret that has been
disclosed — the old value survives in backups and support bundles. Migration
ends with rotating the credential, not with moving it.
