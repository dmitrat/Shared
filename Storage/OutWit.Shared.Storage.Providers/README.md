# OutWit.Shared.Storage.Providers

Blob-storage abstractions for OutWit hosts. Defines a low-level
`IBlobStorageProvider` (Write / Append / Read / ReadChunk / Delete / Exists /
GetSize by `Guid` + filename), the `IBlobStorageProviderPlugin` plugin
contract (a marker over `IWitPlugin` so transports load via `WitPluginLoader`),
the `IBlobStorageSettings` configuration contract, and MemoryPack-friendly
`BlobInfo` / `BlobUploadSession` models.

This is the **contract package** — install one of the provider plugins to
get an actual transport:

- [`OutWit.Shared.Storage.Provider.Disk`](https://www.nuget.org/packages/OutWit.Shared.Storage.Provider.Disk) — filesystem
- (future) `OutWit.Shared.Storage.Provider.S3`, `OutWit.Shared.Storage.Provider.Azure`, …

## Install

```bash
dotnet add package OutWit.Shared.Storage.Providers
# plus at least one provider plugin
dotnet add package OutWit.Shared.Storage.Provider.Disk
```

## Wire up

```csharp
public sealed class MyStorageSettings : IBlobStorageSettings
{
    public string ProviderKey { get; set; } = "Disk";
    public string PluginsPath { get; set; } = "@Storage";
    public long MaxBlobSize { get; set; } = 5L * 1024 * 1024 * 1024; // 5 GB
    public int ChunkSize { get; set; } = 4 * 1024 * 1024;            // 4 MB
    public long ChunkedTransferThresholdBytes { get; set; } = 16 * 1024 * 1024;
    public int DefaultTtlMinutes { get; set; } = 60 * 24 * 7;        // 7 days
    public int CleanupIntervalMinutes { get; set; } = 15;
    public int UploadSessionTimeoutMinutes { get; set; } = 30;
}

services.AddBlobStorage(new MyStorageSettings(), environment);

// later, inside any service:
public class MyService
{
    private readonly IBlobStorageProvider m_storage;
    public MyService(IBlobStorageProvider storage) => m_storage = storage;

    public async Task UploadAsync(byte[] data)
    {
        var blobId = Guid.NewGuid();
        await using var s = new MemoryStream(data);
        await m_storage.WriteAsync(blobId, "data.bin", s);
    }
}
```

`AddBlobStorage` loads every plugin under `PluginsPath` (defaults to
`@Storage` next to the executable), picks the one whose `Key` matches
`Settings.ProviderKey`, and registers its `IBlobStorageProvider` in DI.

## API

| Type | Purpose |
|---|---|
| `IBlobStorageProvider` | Low-level Write / Read / Delete / Exists / GetSize |
| `IBlobStorageProviderPlugin : IWitPlugin` | Plugin contract that the host's `WitPluginLoader<T>` discovers |
| `IBlobStorageSettings` | Required configuration (provider key + chunking + TTL + sessions) |
| `BlobInfo` | `[MemoryPackable]` model — `Id`, `FileName`, `Size`, `CreatedAtUtc`, `ExpiresAtUtc?` |
| `BlobUploadSession` | DTO for chunked-upload session state |
| `StorageUtils.AddBlobStorage` | DI entry point |

## License

Licensed under the Apache License, Version 2.0. See `LICENSE.txt`.

## Trademark / Project name

"OutWit" and the OutWit logo are used to identify the official project by
Dmitry Ratner. You may refer to the project name in a factual way; you
may not use the name or logo to imply official endorsement of a fork or
derived product.
