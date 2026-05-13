namespace OutWit.Shared.Email.Provider.Smtp
{
    /// <summary>
    /// Transport-layer security mode for SMTP connections. Maps 1:1 to MailKit's
    /// <c>SecureSocketOptions</c> at send time.
    /// </summary>
    public enum SmtpSecurity
    {
        /// <summary>
        /// Plain SMTP, no TLS. Typically port 25, dev / on-prem only.
        /// </summary>
        None,

        /// <summary>
        /// Connect plain, upgrade to TLS via STARTTLS command. Typical
        /// "submission" port 587. Most common for modern providers.
        /// </summary>
        StartTls,

        /// <summary>
        /// TLS from the first byte (implicit TLS). Typical port 465.
        /// </summary>
        SslOnConnect,

        /// <summary>
        /// Let MailKit decide based on what the server advertises. Convenient
        /// but less explicit — prefer one of the others in production.
        /// </summary>
        Auto
    }
}
