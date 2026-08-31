# OutWit.Shared.Secrets.Conformance

The conformance suite for
[`OutWit.Shared.Secrets.Providers`](https://www.nuget.org/packages/OutWit.Shared.Secrets.Providers)
implementations: NUnit fixtures a provider's test project inherits and points at
its own store. The abstraction's promises — the status model, atomic replace,
the size and key limits, case injectivity of the platform mapping — are tested
against every implementation by that implementation's own author, rather than
restated in each one's tests.

## Use

```csharp
[TestFixture]
public class MyStoreConformanceTests : SecretStoreConformanceTests
{
    protected override ISecretStore CreateStore() => new MyStore(...);
}
```

That is the whole integration: every promise runs against your store. A provider
whose `Description.CanWrite` is false runs the read-side tests only; the
mutation tests are skipped, not failed.

What the suite covers, and why each row is there:

| Property | Why |
|---|---|
| Store→read round-trips the same bytes | The floor |
| Store over an existing key replaces | Rotation depends on it |
| Read of an absent key is `NotFound`, not `Failed`, not an exception | The status-model conflation this subsystem exists to prevent |
| Delete of an absent key succeeds | Uninstall must be idempotent |
| Delete→read is `NotFound` | Untested deletes outlive uninstallers |
| The documented maximum round-trips; one byte over fails naming the limit | The figure is established here rather than trusted |
| Non-text bytes round-trip unchanged | A random 32-byte key is not a string |
| An empty secret is refused, distinctly from a missing one | `""` and absent must never be confused |
| Concurrent writes leave one whole value | Atomic replace |
| Key rules: invalid throws, at-limit does not | Boundaries |
| Two keys differing only in case are two secrets | The platform mapping must be injective |
| `Description` populated, `Protection` never `Unknown` | A host that cannot see what it got cannot refuse it |

Write the provider-specific tests beside it: for Windows, that a *different
account* cannot read the entry (needs a second account — arrange for it on a
self-hosted agent rather than dropping it); for a file store, that permissions
read back correctly and a crash between write and rename leaves the previous
value; for libsecret, that no session bus yields `Unavailable`.
