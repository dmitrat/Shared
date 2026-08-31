# OutWit.Shared.Secrets.Provider.MacOS

macOS Keychain provider for
[`OutWit.Shared.Secrets.Providers`](https://www.nuget.org/packages/OutWit.Shared.Secrets.Providers):
generic passwords in the **login keychain**, service `com.outwit.secrets`,
account = the key. Payloads are binary-safe; an existing item is replaced in
place (`SecKeychainItemModifyAttributesAndData`) — there is no delete-then-add
window in which a crash leaves nothing.

A support engineer finds entries in *Keychain Access → login*, searching for
`com.outwit.secrets`.

## The ACL, stated prominently

The keychain names the binary that created an item. **An item added by one
binary and read by another prompts the user — and a prompt in a service is a
hang.** Keep the storing and the reading binary the same (the normal case: the
application stores its own credential), or expect the prompt after an update
that changes the binary's identity — sign the app so the identity survives
updates.

This provider is for **user-facing applications** and uses the login keychain.
A daemon belongs in the System keychain with an ACL story of its own; that is a
separate provider when somebody needs it, not a mode of this one.

## Use

```csharp
services.AddSingleton<ISecretStore>(new SecretStoreKeychain());
// or, for a cross-platform desktop app: OutWit.Shared.Secrets.Desktop
```
