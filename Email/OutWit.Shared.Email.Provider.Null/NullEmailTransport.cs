using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Common.Email;

namespace OutWit.Shared.Email.Provider.Null
{
    /// <summary>
    /// Fallback <see cref="IEmailTransport"/> for deployments that don't have an
    /// outbound email account configured. Behaviour controlled by <see cref="NullEmailMode"/>.
    /// </summary>
    public sealed class NullEmailTransport : IEmailTransport
    {
        #region Constants

        private const int LOG_EXCERPT_MAX_LENGTH = 200;

        #endregion

        #region Constructors

        public NullEmailTransport(NullEmailMode mode, ILogger<NullEmailTransport> logger)
        {
            m_mode = mode;
            m_logger = logger;
        }

        #endregion

        #region Fields

        private readonly NullEmailMode m_mode;

        private readonly ILogger<NullEmailTransport> m_logger;

        #endregion

        #region IEmailTransport

        public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            if (m_mode == NullEmailMode.LogOnly)
            {
                m_logger.LogWarning(
                    "[EMAIL NOT SENT — Null provider in LogOnly mode] To={To} Subject={Subject} Body={Excerpt}",
                    message.To, message.Subject, FirstLine(message.HtmlBody, LOG_EXCERPT_MAX_LENGTH));

                return Task.FromResult(EmailSendResult.Success(
                    providerMessageId: $"null-{Guid.NewGuid():N}"));
            }

            m_logger.LogError(
                "Email to {To} not sent: Null email provider is active in Drop mode " +
                "(no outbound email is configured for this deployment).",
                message.To);

            return Task.FromResult(EmailSendResult.Failure(
                EmailFailureKind.Permanent,
                "No email provider is configured for this deployment."));
        }

        #endregion

        #region Tools

        private static string FirstLine(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var newline = text.IndexOf('\n');
            var line = newline >= 0 ? text.Substring(0, newline) : text;
            return line.Length > maxLength ? line.Substring(0, maxLength) + "…" : line;
        }

        #endregion

        #region Properties

        public NullEmailMode Mode => m_mode;

        #endregion
    }
}
