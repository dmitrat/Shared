using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Resend;
using OutWitEmail = OutWit.Common.Email;
using ResendEmail = Resend.EmailMessage;

namespace OutWit.Shared.Email.Provider.Resend
{
    /// <summary>
    /// Resend-SDK-backed <see cref="OutWitEmail.IEmailTransport"/>. Translates the
    /// neutral <see cref="OutWitEmail.EmailMessage"/> into Resend's wire format,
    /// dispatches via <see cref="IResend.EmailSendAsync(Resend.EmailMessage, CancellationToken)"/>,
    /// and maps exceptions to <see cref="OutWitEmail.EmailFailureKind"/> via
    /// <see cref="ResendFailureClassifier"/>.
    /// </summary>
    public sealed class ResendEmailTransport : OutWitEmail.IEmailTransport
    {
        #region Constructors

        public ResendEmailTransport(IResend resend, ILogger<ResendEmailTransport> logger)
        {
            m_resend = resend ?? throw new ArgumentNullException(nameof(resend));
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Fields

        private readonly IResend m_resend;

        private readonly ILogger<ResendEmailTransport> m_logger;

        #endregion

        #region IEmailTransport

        public async Task<OutWitEmail.EmailSendResult> SendAsync(OutWitEmail.EmailMessage message, CancellationToken ct = default)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));

            var payload = BuildResendMessage(message);

            try
            {
                var response = await m_resend.EmailSendAsync(payload, ct).ConfigureAwait(false);
                return OutWitEmail.EmailSendResult.Success(providerMessageId: response.Content.ToString());
            }
            catch (Exception ex)
            {
                var kind = ResendFailureClassifier.Classify(ex);
                m_logger.LogWarning(ex,
                    "Resend send to {To} failed ({Kind}): {Message}",
                    message.To, kind, ex.Message);
                return OutWitEmail.EmailSendResult.Failure(kind, ex.Message);
            }
        }

        #endregion

        #region Tools

        private static ResendEmail BuildResendMessage(OutWitEmail.EmailMessage source)
        {
            var msg = new ResendEmail
            {
                From = source.From,
                To = source.To,
                Subject = source.Subject,
                HtmlBody = source.HtmlBody
            };

            if (!string.IsNullOrEmpty(source.TextBody))
                msg.TextBody = source.TextBody;

            if (!string.IsNullOrEmpty(source.ReplyTo))
                msg.ReplyTo = source.ReplyTo;

            if (source.Headers != null && source.Headers.Count > 0)
            {
                msg.Headers = new Dictionary<string, string>(source.Headers.Count);
                foreach (var kvp in source.Headers)
                    msg.Headers[kvp.Key] = kvp.Value;
            }

            return msg;
        }

        #endregion
    }
}
