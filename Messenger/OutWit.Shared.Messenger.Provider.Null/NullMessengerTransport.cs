using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Common.Messenger;

namespace OutWit.Shared.Messenger.Provider.Null
{
    /// <summary>
    /// Fallback <see cref="IMessengerTransport"/> for deployments that don't have a
    /// messenger configured. Behaviour controlled by <see cref="NullMessengerMode"/>.
    /// </summary>
    public sealed class NullMessengerTransport : IMessengerTransport
    {
        #region Constants

        private const int LOG_EXCERPT_MAX_LENGTH = 200;

        #endregion

        #region Constructors

        public NullMessengerTransport(NullMessengerMode mode, ILogger<NullMessengerTransport> logger)
        {
            m_mode = mode;
            m_logger = logger;
        }

        #endregion

        #region Fields

        private readonly NullMessengerMode m_mode;

        private readonly ILogger<NullMessengerTransport> m_logger;

        #endregion

        #region IMessengerTransport

        public Task<MessageSendResult> SendAsync(MessengerMessage message, CancellationToken ct = default)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            if (m_mode == NullMessengerMode.LogOnly)
            {
                m_logger.LogWarning(
                    "[MESSAGE NOT SENT — Null messenger provider in LogOnly mode] Target={Target} Text={Excerpt}",
                    message.Target, FirstLine(message.RenderText(), LOG_EXCERPT_MAX_LENGTH));

                return Task.FromResult(MessageSendResult.Success(
                    providerMessageId: $"null-{Guid.NewGuid():N}"));
            }

            m_logger.LogError(
                "Message to {Target} not sent: Null messenger provider is active in Drop mode " +
                "(no messenger is configured for this deployment).",
                message.Target);

            return Task.FromResult(MessageSendResult.Failure(
                MessengerFailureKind.Permanent,
                "No messenger provider is configured for this deployment."));
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

        public NullMessengerMode Mode => m_mode;

        #endregion
    }
}
