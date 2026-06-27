# OutWit.Shared.Messenger.Provider.Telegram

Telegram transport plugin for the OutWit messenger subsystem. Sends notifications via
the Telegram **Bot HTTP API** (`https://api.telegram.org/bot<token>/sendMessage`) —
no third-party SDK, just `HttpClient`.

A single bot can post to **any number of chats / channels**: each
`MessengerMessage.Target` is a chat id / `@channel`, so a host can route different
notification categories to different Telegram channels through one bot.

## Configuration

The plugin reads its own `appsettings.json` (env-var overrides via `__`):

```json
{ "Telegram": { "BotToken": "", "DefaultChatId": "", "ApiBaseUrl": "https://api.telegram.org" } }
```

- `Telegram:BotToken` (env `Telegram__BotToken`) — bot token from `@BotFather`. Required.
- `Telegram:DefaultChatId` — fallback target when a message has no `Target`.

Drop into the host's `@Messenger` folder and set `Messenger__ProviderKey=Telegram`.

## Failure handling

HTTP/API errors map to `MessengerFailureKind`: `401` → `AuthFailure`, `403` /
"chat not found" / "bot was blocked" → `InvalidRecipient`, `429` → `RateLimited`,
`5xx` / network → `Transient`, other `400` → `Permanent`.
