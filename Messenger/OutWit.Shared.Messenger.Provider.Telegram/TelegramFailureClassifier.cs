using System;
using System.Net.Http;
using System.Threading.Tasks;
using OutWit.Common.Messenger;

namespace OutWit.Shared.Messenger.Provider.Telegram
{
    /// <summary>
    /// Maps transport exceptions and Telegram Bot API responses to neutral
    /// <see cref="MessengerFailureKind"/> values. Static — unit-tested without live HTTP.
    /// </summary>
    public static class TelegramFailureClassifier
    {
        #region Functions

        /// <summary>
        /// Classifies an exception thrown by the HTTP stack while talking to Telegram.
        /// Walks <see cref="Exception.InnerException"/> too — transport-level failures
        /// are often wrapped.
        /// </summary>
        public static MessengerFailureKind Classify(Exception exception)
        {
            for (Exception? ex = exception; ex != null; ex = ex.InnerException)
            {
                switch (ex)
                {
                    case HttpRequestException:       return MessengerFailureKind.Transient;
                    case TaskCanceledException:      return MessengerFailureKind.Transient;
                    case TimeoutException:           return MessengerFailureKind.Transient;
                    case OperationCanceledException: return MessengerFailureKind.Transient;
                }
            }
            return MessengerFailureKind.Permanent;
        }

        /// <summary>
        /// Maps a Telegram Bot API failure (HTTP status code + the <c>description</c>
        /// from the error body) to a neutral failure kind. The description is inspected
        /// first because Telegram returns <c>400</c>/<c>403</c> for several distinct
        /// recipient problems.
        /// </summary>
        public static MessengerFailureKind ClassifyResponse(int statusCode, string? description)
        {
            var desc = description?.ToLowerInvariant() ?? string.Empty;

            if (desc.Contains("chat not found")
                || desc.Contains("user not found")
                || desc.Contains("bot was blocked")
                || desc.Contains("bot was kicked")
                || desc.Contains("user is deactivated")
                || desc.Contains("chat_id is empty")
                || desc.Contains("group chat was upgraded"))
                return MessengerFailureKind.InvalidRecipient;

            return statusCode switch
            {
                401               => MessengerFailureKind.AuthFailure,      // bad bot token
                403               => MessengerFailureKind.InvalidRecipient, // blocked / kicked
                429               => MessengerFailureKind.RateLimited,
                400               => MessengerFailureKind.Permanent,        // malformed request
                >= 500 and <= 599 => MessengerFailureKind.Transient,
                _                 => MessengerFailureKind.Permanent
            };
        }

        #endregion
    }
}
