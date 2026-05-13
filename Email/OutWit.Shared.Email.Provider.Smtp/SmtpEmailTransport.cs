using System;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using OutWit.Common.Email;

namespace OutWit.Shared.Email.Provider.Smtp
{
    /// <summary>
    /// MailKit-backed <see cref="IEmailTransport"/>. Builds a MIME message
    /// from the neutral <see cref="EmailMessage"/>, connects to the configured
    /// SMTP server, authenticates if creds were supplied, sends, disconnects.
    /// Maps exceptions to <see cref="EmailFailureKind"/> via
    /// <see cref="SmtpFailureClassifier"/>.
    /// </summary>
    public sealed class SmtpEmailTransport : IEmailTransport
    {
        #region Constructors

        public SmtpEmailTransport(SmtpOptions options, ILogger<SmtpEmailTransport> logger)
        {
            m_options = options ?? throw new ArgumentNullException(nameof(options));
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrWhiteSpace(m_options.Host))
                throw new InvalidOperationException("SmtpOptions.Host is required.");
        }

        #endregion

        #region Fields

        private readonly SmtpOptions m_options;

        private readonly ILogger<SmtpEmailTransport> m_logger;

        #endregion

        #region IEmailTransport

        public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));

            var mime = BuildMimeMessage(message);

            try
            {
                using var client = new SmtpClient();
                client.Timeout = (int)m_options.Timeout.TotalMilliseconds;

                if (m_options.AllowSelfSignedCertificates)
                    client.ServerCertificateValidationCallback = (_, _, _, _) => true;

                await client.ConnectAsync(m_options.Host, m_options.Port, ToSecureSocketOptions(m_options.Security), ct)
                    .ConfigureAwait(false);

                if (!string.IsNullOrEmpty(m_options.Username))
                {
                    await client.AuthenticateAsync(m_options.Username, m_options.Password ?? string.Empty, ct)
                        .ConfigureAwait(false);
                }

                var serverResponse = await client.SendAsync(mime, ct).ConfigureAwait(false);
                await client.DisconnectAsync(quit: true, ct).ConfigureAwait(false);

                return EmailSendResult.Success(providerMessageId: serverResponse);
            }
            catch (Exception ex)
            {
                var kind = SmtpFailureClassifier.Classify(ex);
                m_logger.LogWarning(ex,
                    "SMTP send to {To} failed via {Host}:{Port} ({Kind}): {Message}",
                    message.To, m_options.Host, m_options.Port, kind, ex.Message);
                return EmailSendResult.Failure(kind, ex.Message);
            }
        }

        #endregion

        #region Tools

        private static MimeMessage BuildMimeMessage(EmailMessage message)
        {
            var mime = new MimeMessage();
            mime.From.Add(MailboxAddress.Parse(message.From));
            mime.To.Add(MailboxAddress.Parse(message.To));
            if (!string.IsNullOrEmpty(message.ReplyTo))
                mime.ReplyTo.Add(MailboxAddress.Parse(message.ReplyTo));
            mime.Subject = message.Subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = message.HtmlBody };
            if (!string.IsNullOrEmpty(message.TextBody))
                bodyBuilder.TextBody = message.TextBody;
            mime.Body = bodyBuilder.ToMessageBody();

            if (message.Headers != null)
            {
                foreach (var kvp in message.Headers)
                    mime.Headers.Add(kvp.Key, kvp.Value);
            }

            return mime;
        }

        private static SecureSocketOptions ToSecureSocketOptions(SmtpSecurity security) => security switch
        {
            SmtpSecurity.None         => SecureSocketOptions.None,
            SmtpSecurity.StartTls     => SecureSocketOptions.StartTls,
            SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
            SmtpSecurity.Auto         => SecureSocketOptions.Auto,
            _                         => SecureSocketOptions.StartTls
        };

        #endregion
    }
}
