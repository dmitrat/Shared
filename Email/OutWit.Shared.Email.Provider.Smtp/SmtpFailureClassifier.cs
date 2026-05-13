using System;
using System.IO;
using System.Net.Sockets;
using MailKit.Net.Smtp;
using MailKit.Security;
using OutWit.Common.Email;

namespace OutWit.Shared.Email.Provider.Smtp
{
    /// <summary>
    /// Maps MailKit exceptions and SMTP status codes to neutral
    /// <see cref="EmailFailureKind"/> values. Static — unit-tested without
    /// needing a live SMTP server.
    /// </summary>
    public static class SmtpFailureClassifier
    {
        /// <summary>
        /// Classifies an exception thrown by MailKit during connect / authenticate / send.
        /// </summary>
        public static EmailFailureKind Classify(Exception exception)
        {
            return exception switch
            {
                AuthenticationException             => EmailFailureKind.AuthFailure,
                SmtpCommandException cmd            => ClassifySmtpCommand(cmd),
                IOException                         => EmailFailureKind.Transient,
                SocketException                     => EmailFailureKind.Transient,
                TimeoutException                    => EmailFailureKind.Transient,
                OperationCanceledException          => EmailFailureKind.Transient,
                _                                   => EmailFailureKind.Permanent
            };
        }

        /// <summary>
        /// Maps an SMTP server status code to a failure kind.
        /// </summary>
        /// <remarks>
        /// Reference: RFC 5321 reply codes. The mapping prioritizes whether the
        /// caller should retry (Transient / RateLimited) vs accept the loss
        /// (InvalidRecipient / Permanent).
        /// </remarks>
        public static EmailFailureKind ClassifyStatusCode(int code)
        {
            return code switch
            {
                // 4xx — transient; server says "try again later"
                421 or 450 or 451 or 452 => EmailFailureKind.Transient,
                // 5xx authentication
                535                      => EmailFailureKind.AuthFailure,
                // 5xx bad recipient
                550 or 551 or 553        => EmailFailureKind.InvalidRecipient,
                // 552 — message too large
                552                      => EmailFailureKind.Permanent,
                // Anything else 5xx → don't retry
                _                        => EmailFailureKind.Permanent
            };
        }

        private static EmailFailureKind ClassifySmtpCommand(SmtpCommandException cmd)
        {
            // MailKit's SmtpStatusCode is the SMTP reply code (RFC 5321).
            return ClassifyStatusCode((int)cmd.StatusCode);
        }
    }
}
