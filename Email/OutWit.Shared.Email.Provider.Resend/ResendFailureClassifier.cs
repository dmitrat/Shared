using System;
using System.Net.Http;
using System.Threading.Tasks;
using OutWit.Common.Email;
using Resend;

namespace OutWit.Shared.Email.Provider.Resend
{
    /// <summary>
    /// Maps Resend SDK exceptions and HTTP status codes to neutral
    /// <see cref="EmailFailureKind"/> values. Static — unit-tested without
    /// needing live HTTP calls.
    /// </summary>
    public static class ResendFailureClassifier
    {
        #region Functions

        /// <summary>
        /// Classifies an exception thrown by the Resend SDK or underlying HTTP stack.
        /// Walks <see cref="Exception.InnerException"/> too — the SDK sometimes
        /// wraps transport-level <see cref="HttpRequestException"/>s in a generic
        /// exception, so a naive type-switch on the top-level type would miss them.
        /// </summary>
        public static EmailFailureKind Classify(Exception exception)
        {
            for (Exception? ex = exception; ex != null; ex = ex.InnerException)
            {
                switch (ex)
                {
                    case ResendException re:        return ClassifyResendException(re);
                    case HttpRequestException:      return EmailFailureKind.Transient;
                    case TaskCanceledException:     return EmailFailureKind.Transient;
                    case TimeoutException:          return EmailFailureKind.Transient;
                    case OperationCanceledException: return EmailFailureKind.Transient;
                }
            }
            return EmailFailureKind.Permanent;
        }

        /// <summary>
        /// Maps a Resend <see cref="ErrorType"/> (returned in the API error body) to
        /// a neutral failure kind. Prefers the typed error over the HTTP status code
        /// since the SDK exposes both — the typed code is more specific.
        /// </summary>
        public static EmailFailureKind ClassifyErrorType(ErrorType errorType)
        {
            return errorType switch
            {
                // Authentication / authorization
                ErrorType.MissingApiKey
                    or ErrorType.InvalidApiKey
                    or ErrorType.RestrictedApiKey
                    or ErrorType.InvalidAccess     => EmailFailureKind.AuthFailure,

                // Throttling / quota — caller should back off and retry
                ErrorType.RateLimitExceeded
                    or ErrorType.MonthlyQuotaExceeded
                    or ErrorType.DailyQuotaExceeded => EmailFailureKind.RateLimited,

                // Server-side hiccups + transport failures — caller may retry
                ErrorType.InternalServerError
                    or ErrorType.ApplicationError
                    or ErrorType.ConcurrentIdempotentRequests
                    or ErrorType.HttpSendFailed
                    or ErrorType.MissingResponse  => EmailFailureKind.Transient,

                // Everything else (validation, bad from, bad params, security,
                // deserialization, idempotency mismatches) is a permanent client-
                // side defect — retrying won't fix it.
                _                                 => EmailFailureKind.Permanent
            };
        }

        /// <summary>
        /// Maps a raw HTTP status code returned by the Resend API to a failure kind.
        /// Used as a fallback when <see cref="ResendException.ErrorType"/> doesn't
        /// give us enough context (e.g. transport-layer 5xx without a typed body).
        /// </summary>
        public static EmailFailureKind ClassifyStatusCode(int code)
        {
            return code switch
            {
                401 or 403           => EmailFailureKind.AuthFailure,
                422                  => EmailFailureKind.Permanent,        // validation error
                429                  => EmailFailureKind.RateLimited,
                >= 500 and <= 599    => EmailFailureKind.Transient,
                _                    => EmailFailureKind.Permanent
            };
        }

        #endregion

        #region Tools

        private static EmailFailureKind ClassifyResendException(ResendException ex)
        {
            // Use the explicit ErrorType bucketing first — it covers known auth,
            // quota, validation, and server-side errors precisely.
            var byType = ClassifyErrorType(ex.ErrorType);
            if (byType != EmailFailureKind.Permanent)
                return byType;

            // ErrorType landed on Permanent. Two escape hatches before we commit:
            // 1. The SDK's own retriability hint catches transport-level codes
            //    (HttpSendFailed) we may not have mapped explicitly.
            if (ex.IsTransient)
                return EmailFailureKind.Transient;

            // 2. As a last resort, peek at the HTTP status. SDK 0.5.0 reports
            //    StatusCode = 0 in many cases, so guard against that.
            if (ex.StatusCode.HasValue && (int)ex.StatusCode.Value > 0)
            {
                var byCode = ClassifyStatusCode((int)ex.StatusCode.Value);
                if (byCode != EmailFailureKind.Permanent)
                    return byCode;
            }

            return EmailFailureKind.Permanent;
        }

        #endregion
    }
}
