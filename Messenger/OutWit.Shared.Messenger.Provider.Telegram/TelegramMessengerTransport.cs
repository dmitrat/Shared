using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Common.Messenger;

namespace OutWit.Shared.Messenger.Provider.Telegram
{
    /// <summary>
    /// <see cref="IMessengerTransport"/> backed by the Telegram Bot HTTP API
    /// (<c>https://api.telegram.org/bot&lt;token&gt;/sendMessage</c>). Sends to an
    /// arbitrary <see cref="MessengerMessage.Target"/> (chat id / <c>@channel</c>) so
    /// the host can route to many named channels through a single bot. Maps HTTP /
    /// API failures to <see cref="MessengerFailureKind"/> via
    /// <see cref="TelegramFailureClassifier"/>.
    /// </summary>
    public sealed class TelegramMessengerTransport : IMessengerTransport
    {
        #region Constructors

        public TelegramMessengerTransport(HttpClient httpClient, TelegramOptions options,
            ILogger<TelegramMessengerTransport> logger)
        {
            m_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            m_options = options ?? throw new ArgumentNullException(nameof(options));
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Fields

        // Telegram message text routinely contains emojis / non-Latin scripts. Use the
        // relaxed encoder so they go on the wire as literal UTF-8 rather than \uXXXX
        // escapes (smaller, readable payload; Telegram decodes both identically).
        private static readonly JsonSerializerOptions JSON_OPTIONS = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly HttpClient m_httpClient;

        private readonly TelegramOptions m_options;

        private readonly ILogger<TelegramMessengerTransport> m_logger;

        #endregion

        #region IMessengerTransport

        public async Task<MessageSendResult> SendAsync(MessengerMessage message, CancellationToken ct = default)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            var chatId = !string.IsNullOrWhiteSpace(message.Target)
                ? message.Target
                : m_options.DefaultChatId;

            if (string.IsNullOrWhiteSpace(chatId))
                return MessageSendResult.Failure(MessengerFailureKind.InvalidRecipient,
                    "No target chat id supplied and no DefaultChatId configured.");

            if (string.IsNullOrWhiteSpace(m_options.BotToken))
                return MessageSendResult.Failure(MessengerFailureKind.AuthFailure,
                    "Telegram bot token is not configured.");

            var url = $"{m_options.ApiBaseUrl.TrimEnd('/')}/bot{m_options.BotToken}/sendMessage";

            var payload = new Dictionary<string, object?>
            {
                ["chat_id"] = chatId,
                ["text"] = message.RenderText(),
                ["disable_notification"] = message.SilentNotification
            };

            var parseMode = ToParseMode(message.Format);
            if (parseMode != null)
                payload["parse_mode"] = parseMode;

            try
            {
                using var content = new StringContent(
                    JsonSerializer.Serialize(payload, JSON_OPTIONS), Encoding.UTF8, "application/json");

                using var response = await m_httpClient.PostAsync(url, content, ct).ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

                return ParseResponse((int)response.StatusCode, body, chatId!);
            }
            catch (Exception ex)
            {
                var kind = TelegramFailureClassifier.Classify(ex);
                m_logger.LogWarning(ex, "Telegram send to {Target} failed ({Kind}): {Message}",
                    chatId, kind, ex.Message);
                return MessageSendResult.Failure(kind, ex.Message);
            }
        }

        #endregion

        #region Tools

        private MessageSendResult ParseResponse(int statusCode, string body, string chatId)
        {
            var ok = false;
            string? description = null;
            string? messageId = null;

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.TryGetProperty("ok", out var okEl))
                    ok = okEl.ValueKind == JsonValueKind.True;

                if (root.TryGetProperty("description", out var descEl))
                    description = descEl.GetString();

                if (root.TryGetProperty("result", out var resEl)
                    && resEl.ValueKind == JsonValueKind.Object
                    && resEl.TryGetProperty("message_id", out var midEl))
                    messageId = midEl.ToString();
            }
            catch (JsonException)
            {
                // Non-JSON body (e.g. an HTML error page from a proxy) — fall through
                // to status-code classification below.
            }

            if (ok)
            {
                m_logger.LogInformation("Telegram message sent to {Target} (message_id={MessageId})",
                    chatId, messageId);
                return MessageSendResult.Success(messageId);
            }

            var kind = TelegramFailureClassifier.ClassifyResponse(statusCode, description);
            m_logger.LogWarning("Telegram send to {Target} failed ({Kind}, HTTP {Status}): {Description}",
                chatId, kind, statusCode, description);
            return MessageSendResult.Failure(kind, description ?? $"HTTP {statusCode}");
        }

        private static string? ToParseMode(MessageFormat format)
        {
            return format switch
            {
                MessageFormat.Markdown => "MarkdownV2",
                MessageFormat.Html => "HTML",
                _ => null
            };
        }

        #endregion
    }
}
