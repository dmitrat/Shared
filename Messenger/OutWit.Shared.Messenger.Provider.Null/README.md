# OutWit.Shared.Messenger.Provider.Null

Zero-config fallback messenger plugin for OutWit hosts — the counterpart of the
Null email provider.

Two modes (from the plugin's own `appsettings.json` `Null:Mode`, or `Null__Mode`):

- **LogOnly** (default) — logs the target + first line of the text at `Warning` and
  returns success. Useful in dev / staging before a real messenger is wired up.
- **Drop** — logs an error and returns a `Permanent` failure. Useful for deployments
  that genuinely don't need messenger notifications.

Drop into the host's `@Messenger` folder and set `Messenger__ProviderKey=Null`.
