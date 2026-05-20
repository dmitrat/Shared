# OutWit.Shared.Storage.Provider.Disk

Filesystem-backed blob storage plugin for OutWit hosts. Implements
`IBlobStorageProvider` against the local disk: each blob lives in its
own GUID-named directory under a configurable root, with one or more
files inside.

Drop this package into any consumer of
[`OutWit.Shared.Storage.Providers`](https://www.nuget.org/packages/OutWit.Shared.Storage.Providers)
and set `Storage:ProviderKey=Disk` — the host's `WitPluginLoader` picks
the plugin up at startup.

## Install

```bash
dotnet add package OutWit.Shared.Storage.Provider.Disk
```

A NuGet post-build target stages the plugin module into the consuming
project's output directory at `@Storage/disk.module/`, where the host's
`WitPluginLoader<IBlobStorageProviderPlugin>` discovers it.

## Configure

The plugin ships an `appsettings.json` with a single key:

```json
{
  "DiskBlobStorage": {
    "StoragePath": "@Blobs"
  }
}
```

- Relative paths resolve from `AppContext.BaseDirectory`.
- The directory is created on first use if missing.
- Per-host overrides via `appsettings.{Environment}.json` and standard
  environment variables (`DiskBlobStorage__StoragePath=...`).

## Layout on disk

For blob `Guid` = `0e3c…d1`:

```
@Blobs/
  0e3c…d1/
    archive.zip
    readme.md
```

Multiple files per blob are supported. The provider validates filenames
for path traversal (`..`, separators, invalid chars) — uploads outside
the blob's GUID directory cannot happen.

## Limitations

- **Single-machine** — no replication or sharding. Use S3 / Azure Blob
  plugins (planned) for multi-node or cloud deployments.
- **Locking is per-file** — concurrent writes to the same blob+filename
  serialise via `FileShare.None`; concurrent writes to different files
  proceed in parallel.
- **No background cleanup** — blob expiry is the responsibility of the
  host's higher-level `BlobStorageService` (in WitCloud) reading
  `BlobInfo.ExpiresAtUtc` and calling `DeleteAsync`.

## License

Licensed under the Apache License, Version 2.0. See `LICENSE.txt`.
