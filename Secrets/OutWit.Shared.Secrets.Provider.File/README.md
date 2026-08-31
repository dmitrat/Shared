# OutWit.Shared.Secrets.Provider.File

The explicitly-labelled file fallback for
[`OutWit.Shared.Secrets.Providers`](https://www.nuget.org/packages/OutWit.Shared.Secrets.Providers) —
for deployments where no OS credential store is reachable: containers,
session-less Linux services. **Never selected automatically.** Configuration
names this provider; a store that silently degrades is worse than one that
fails, because the degradation is invisible exactly when it matters.

## What it does

- One file per key — `{key with '/'→'.'}-{hash8}.wsecret` — under a directory
  you name; `SecretStoreFile.MapFileName(key)` computes the name.
- **Owner-only permissions applied at creation and verified by reading them
  back**: `0600` on POSIX; on Windows an explicit non-inherited DACL naming only
  the owning account, part of the create call itself. Creating a file and hoping
  the parent directory's ACL is sane is how a `ProgramData` subtree ends up
  letting any authenticated user replace a credential.
- **Atomic replace**: temp file in the same directory, write-through flush, then
  rename over. A crash mid-rotation leaves the old secret or the new one — never
  neither, never half.
- On Windows the payload is protected with **DPAPI machine scope** (entropy
  bound to the key), and `Description.Protection` says `FileWithPlatformKey`.
  Elsewhere there is no platform key and the description says `FileOnly` —
  honestly. A host that requires better refuses to run on it.

```csharp
services.AddSingleton<ISecretStore>(new SecretStoreFile(new SecretStoreFileOptions
{
    DirectoryPath = "/var/lib/myservice/secrets"
}));

// and log what you got:
logger.LogInformation("Secret store: {Description}", store.Description);
```

## What this does not defend against

The same list as the abstractions README, and on `FileOnly` one more thing:
anyone who can read the file can read the secret — the ACL is the whole
defence. It does not defend against an administrator; an attacker running as
the owning account; an offline attack on a stolen disk or image (the DPAPI
machine key travels with the image); cloning. Keep anything stored here
centrally revocable.
