namespace OutWit.Shared.Messenger.Provider.Telegram
{
    /// <summary>
    /// Configuration for the Telegram messenger transport. Bound from the plugin's
    /// own <c>appsettings.json</c> + environment variables (standard .NET configuration
    /// binding — <c>Telegram__BotToken</c> overrides <c>Telegram:BotToken</c>).
    /// </summary>
    public sealed class TelegramOptions
    {
        #region Constants

        /// <summary>Default Telegram Bot API base URL.</summary>
        public const string DEFAULT_API_URL = "https://api.telegram.org";

        #endregion

        #region Properties

        /// <summary>
        /// Telegram bot token (from <c>@BotFather</c>). Required. Should be supplied
        /// via env var <c>Telegram__BotToken</c> — keep the JSON value blank.
        /// </summary>
        public string? BotToken { get; set; }

        /// <summary>
        /// Optional default chat/target id used when a <c>MessengerMessage.Target</c>
        /// is not supplied. The host normally targets a specific channel per message.
        /// </summary>
        public string? DefaultChatId { get; set; }

        /// <summary>
        /// Base URL for the Telegram Bot API. Defaults to <c>https://api.telegram.org</c>.
        /// Override only to point at a proxy or a local test server.
        /// </summary>
        public string ApiBaseUrl { get; set; } = DEFAULT_API_URL;

        #endregion
    }
}
