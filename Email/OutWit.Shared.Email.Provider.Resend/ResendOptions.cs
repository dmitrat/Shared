namespace OutWit.Shared.Email.Provider.Resend
{
    /// <summary>
    /// Configuration for the Resend email transport. Bound from the plugin's
    /// own <c>appsettings.json</c> + environment variables (standard .NET
    /// configuration binding — <c>Resend__ApiToken</c> overrides <c>Resend:ApiToken</c>).
    /// </summary>
    public sealed class ResendOptions
    {
        #region Constants

        /// <summary>Default Resend API base URL.</summary>
        public const string DEFAULT_API_URL = "https://api.resend.com";

        #endregion

        #region Properties

        /// <summary>
        /// Resend API token. Required. Should be supplied via env var
        /// <c>Resend__ApiToken</c> — keep the JSON value blank.
        /// </summary>
        public string? ApiToken { get; set; }

        /// <summary>
        /// Base URL for the Resend API. Defaults to <c>https://api.resend.com</c>.
        /// Override only to point at a staging endpoint or a self-hosted proxy.
        /// </summary>
        public string ApiUrl { get; set; } = DEFAULT_API_URL;

        #endregion
    }
}
