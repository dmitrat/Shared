using System;

namespace OutWit.Shared.Email.Provider.Smtp
{
    /// <summary>
    /// Configuration for the SMTP email transport. Bound from the plugin's
    /// own <c>appsettings.json</c> + environment variables (standard .NET
    /// configuration binding — <c>Smtp__Password</c> overrides <c>Smtp:Password</c>).
    /// </summary>
    public sealed class SmtpOptions
    {
        #region Constants

        public const int DEFAULT_PORT = 587;

        public static readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromSeconds(30);

        #endregion

        #region Properties

        /// <summary>SMTP server host name. Required.</summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>SMTP server port. Defaults to 587 (submission with STARTTLS).</summary>
        public int Port { get; set; } = DEFAULT_PORT;

        /// <summary>Username for authentication. Leave empty for unauthenticated relays.</summary>
        public string? Username { get; set; }

        /// <summary>Password for authentication. Should come from env var <c>Smtp__Password</c>.</summary>
        public string? Password { get; set; }

        /// <summary>TLS mode. Defaults to <see cref="SmtpSecurity.StartTls"/>.</summary>
        public SmtpSecurity Security { get; set; } = SmtpSecurity.StartTls;

        /// <summary>Connection / send timeout. Defaults to 30 seconds.</summary>
        public TimeSpan Timeout { get; set; } = DEFAULT_TIMEOUT;

        /// <summary>
        /// When <c>true</c>, accept self-signed / untrusted server certificates.
        /// Use only in dev (MailHog, Papercut) — never in production.
        /// </summary>
        public bool AllowSelfSignedCertificates { get; set; }

        #endregion
    }
}
